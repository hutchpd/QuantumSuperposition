using Mono.Cecil;
using Mono.Cecil.Cil;
using PositronicVariables.Variables;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using ToGo.Compiler.Binder;
using ToGo.Compiler.Diagnostics;
using ToGo.Compiler.Lexer;
using ToGo.Compiler.Lowering;
using ToGo.Compiler.Runtime;

using CecilTypeAttributes = Mono.Cecil.TypeAttributes;
using CecilFieldAttributes = Mono.Cecil.FieldAttributes;
using CecilMethodAttributes = Mono.Cecil.MethodAttributes;
using CecilParameterAttributes = Mono.Cecil.ParameterAttributes;

namespace ToGo.Compiler.CodeGen;

public sealed class CecilCodeGenerator
{
    private readonly DiagnosticBag _diagnostics;

    public CecilCodeGenerator(DiagnosticBag diagnostics)
    {
        _diagnostics = diagnostics ?? throw new ArgumentNullException(nameof(diagnostics));
    }

    public void Generate(LoweredProgram program, string outputAssemblyPath)
    {
        if (program.TimeBlocks.Count == 0)
        {
            _diagnostics.Report(0, "No time block found.");
            return;
        }

        string assemblyName = Path.GetFileNameWithoutExtension(outputAssemblyPath);
        var assembly = AssemblyDefinition.CreateAssembly(
            new AssemblyNameDefinition(assemblyName, new Version(0, 1, 0, 0)),
            assemblyName,
            ModuleKind.Console);

        var module = assembly.MainModule;

        var programType = new TypeDefinition(
            "",
            "Program",
            CecilTypeAttributes.Public | CecilTypeAttributes.Abstract | CecilTypeAttributes.Sealed,
            module.ImportReference(typeof(object)));

        module.Types.Add(programType);

        // Static fields for antivals
        var antivalFields = new Dictionary<string, FieldDefinition>(StringComparer.Ordinal);
        var pvIntType = module.ImportReference(typeof(PositronicVariable<int>));

        for (int i = 0; i < program.Antivals.Count; i++)
        {
            var v = program.Antivals[i];
            var field = new FieldDefinition(
                v.Name,
                CecilFieldAttributes.Private | CecilFieldAttributes.Static,
                pvIntType);
            programType.Fields.Add(field);
            antivalFields.Add(v.Name, field);
        }

        // Time methods
        var timeMethods = new List<MethodDefinition>(program.TimeBlocks.Count);
        for (int i = 0; i < program.TimeBlocks.Count; i++)
        {
            var m = EmitTimeMethod(module, programType, antivalFields, program.TimeBlocks[i], i);
            timeMethods.Add(m);
        }

        // Main
        var main = new MethodDefinition(
            "Main",
            CecilMethodAttributes.Public | CecilMethodAttributes.Static,
            module.TypeSystem.Void);
        main.Parameters.Add(new ParameterDefinition("args", CecilParameterAttributes.None, module.ImportReference(typeof(string[]))));
        programType.Methods.Add(main);
        module.EntryPoint = main;

        var il = main.Body.GetILProcessor();

        // Ensure runtime initialized
        il.Append(Instruction.Create(OpCodes.Call, module.ImportReference(typeof(ToGoRuntime).GetMethod(nameof(ToGoRuntime.EnsureInitialised))!)));

        // Instantiate antivals
        var createAntival = typeof(ToGoRuntime).GetMethod(nameof(ToGoRuntime.CreateAntival), new[] { typeof(string), typeof(int) })!;
        var createAntivalRef = module.ImportReference(createAntival);

        for (int i = 0; i < program.Antivals.Count; i++)
        {
            var v = program.Antivals[i];
            var field = antivalFields[v.Name];

            il.Append(Instruction.Create(OpCodes.Ldstr, v.Name));
            il.Append(Instruction.Create(OpCodes.Ldc_I4_0));
            il.Append(Instruction.Create(OpCodes.Call, createAntivalRef));
            il.Append(Instruction.Create(OpCodes.Stsfld, field));
        }

        // Run each time block and then execute deferred prints
        var timeRunnerRun = typeof(TimeRunner).GetMethod(nameof(TimeRunner.Run), new[] { typeof(Action) })!;
        var timeRunnerRunRef = module.ImportReference(timeRunnerRun);
        var actionCtor = typeof(Action).GetConstructor(new[] { typeof(object), typeof(IntPtr) })!;
        var actionCtorRef = module.ImportReference(actionCtor);

        for (int i = 0; i < program.TimeBlocks.Count; i++)
        {
            var timeMethod = timeMethods[i];

            il.Append(Instruction.Create(OpCodes.Ldnull));
            il.Append(Instruction.Create(OpCodes.Ldftn, timeMethod));
            il.Append(Instruction.Create(OpCodes.Newobj, actionCtorRef));
            il.Append(Instruction.Create(OpCodes.Call, timeRunnerRunRef));

            EmitDeferredPrints(module, il, antivalFields, program.TimeBlocks[i].DeferredPrints);
        }

        il.Append(Instruction.Create(OpCodes.Ret));

        assembly.Write(outputAssemblyPath);
    }

    private MethodDefinition EmitTimeMethod(
        ModuleDefinition module,
        TypeDefinition programType,
        Dictionary<string, FieldDefinition> antivalFields,
        LoweredTimeBlock tb,
        int index)
    {
        var method = new MethodDefinition(
            $"__togo_time_{index}",
            CecilMethodAttributes.Private | CecilMethodAttributes.Static,
            module.TypeSystem.Void);

        programType.Methods.Add(method);

        var il = method.Body.GetILProcessor();

        for (int i = 0; i < tb.Assignments.Count; i++)
        {
            EmitAssignment(module, il, antivalFields, tb.Assignments[i]);
        }

        il.Append(Instruction.Create(OpCodes.Ret));
        return method;
    }

    private enum ExprKind
    {
        Int32,
        QExpr
    }

    private readonly struct ExprResult
    {
        public ExprKind Kind { get; }
        public int Int32Value { get; }

        public ExprResult(ExprKind kind, int value)
        {
            Kind = kind;
            Int32Value = value;
        }

        public static ExprResult Int32(int value) => new(ExprKind.Int32, value);
        public static ExprResult QExpr() => new(ExprKind.QExpr, 0);
    }

    private void EmitAssignment(
        ModuleDefinition module,
        ILProcessor il,
        Dictionary<string, FieldDefinition> antivalFields,
        BoundAssignmentStatement a)
    {
        if (!antivalFields.TryGetValue(a.Target.Name, out var targetField))
        {
            _diagnostics.Report(0, $"Unknown antival '{a.Target.Name}'.");
            return;
        }

        var targetType = typeof(PositronicVariable<int>);
        var assignInt = targetType.GetMethod(nameof(PositronicVariable<int>.Assign), new[] { typeof(int) })!;
        var assignIntRef = module.ImportReference(assignInt);

        var qexprType = typeof(PositronicVariable<int>.QExpr);
        var assignQExpr = targetType.GetMethod(nameof(PositronicVariable<int>.Assign), new[] { qexprType })!;
        var assignQExprRef = module.ImportReference(assignQExpr);

        // Load target
        il.Append(Instruction.Create(OpCodes.Ldsfld, targetField));

        var exprRes = EmitExpression(module, il, antivalFields, a.Expression);

        if (exprRes.Kind == ExprKind.Int32)
        {
            il.Append(Instruction.Create(OpCodes.Callvirt, assignIntRef));
        }
        else
        {
            il.Append(Instruction.Create(OpCodes.Callvirt, assignQExprRef));
        }
    }

    private ExprResult EmitExpression(
        ModuleDefinition module,
        ILProcessor il,
        Dictionary<string, FieldDefinition> antivalFields,
        BoundExpression e)
    {
        switch (e)
        {
            case BoundIntegerLiteral lit:
                il.Append(Instruction.Create(OpCodes.Ldc_I4, lit.Value));
                return ExprResult.Int32(lit.Value);

            case BoundVariableExpression v:
                if (!antivalFields.TryGetValue(v.Variable.Name, out var field))
                {
                    _diagnostics.Report(0, $"Unknown antival '{v.Variable.Name}'.");
                    il.Append(Instruction.Create(OpCodes.Ldc_I4_0));
                    return ExprResult.Int32(0);
                }

                var stateGetter = typeof(PositronicVariable<int>).GetProperty(nameof(PositronicVariable<int>.State))!.GetGetMethod()!;
                il.Append(Instruction.Create(OpCodes.Ldsfld, field));
                il.Append(Instruction.Create(OpCodes.Callvirt, module.ImportReference(stateGetter)));
                return ExprResult.QExpr();

            case BoundBinaryExpression bin:
                if (bin.OperatorKind != TokenKind.Plus)
                {
                    _diagnostics.Report(0, $"Only '+' is supported in the MVP.");
                    il.Append(Instruction.Create(OpCodes.Ldc_I4_0));
                    return ExprResult.Int32(0);
                }

                var leftKind = ClassifyExpression(bin.Left);
                var rightKind = ClassifyExpression(bin.Right);

                if (leftKind == ExprKind.Int32 && rightKind == ExprKind.Int32)
                {
                    if (bin.Left is BoundIntegerLiteral li && bin.Right is BoundIntegerLiteral ri)
                    {
                        int sum = li.Value + ri.Value;
                        il.Append(Instruction.Create(OpCodes.Ldc_I4, sum));
                        return ExprResult.Int32(sum);
                    }
                }

                // Emit left
                var left = EmitExpression(module, il, antivalFields, bin.Left);
                // Emit right
                var right = EmitExpression(module, il, antivalFields, bin.Right);

                if (left.Kind == ExprKind.Int32 && right.Kind == ExprKind.Int32)
                {
                    il.Append(Instruction.Create(OpCodes.Add));
                    return ExprResult.Int32(0);
                }

                var qexprType = typeof(PositronicVariable<int>.QExpr);

                if (left.Kind == ExprKind.QExpr && right.Kind == ExprKind.Int32)
                {
                    var add = qexprType.GetMethod(
                        "op_Addition",
                        BindingFlags.Public | BindingFlags.Static,
                        binder: null,
                        types: new[] { qexprType, typeof(int) },
                        modifiers: null)!;
                    il.Append(Instruction.Create(OpCodes.Call, module.ImportReference(add)));
                    return ExprResult.QExpr();
                }

                if (left.Kind == ExprKind.Int32 && right.Kind == ExprKind.QExpr)
                {
                    var add = qexprType.GetMethod(
                        "op_Addition",
                        BindingFlags.Public | BindingFlags.Static,
                        binder: null,
                        types: new[] { typeof(int), qexprType },
                        modifiers: null)!;
                    il.Append(Instruction.Create(OpCodes.Call, module.ImportReference(add)));
                    return ExprResult.QExpr();
                }

                if (left.Kind == ExprKind.QExpr && right.Kind == ExprKind.QExpr)
                {
                    var add = qexprType.GetMethod(
                        "op_Addition",
                        BindingFlags.Public | BindingFlags.Static,
                        binder: null,
                        types: new[] { qexprType, qexprType },
                        modifiers: null)!;
                    il.Append(Instruction.Create(OpCodes.Call, module.ImportReference(add)));
                    return ExprResult.QExpr();
                }

                _diagnostics.Report(0, "Unsupported '+' operands in the MVP.");
                // Attempt to keep stack balanced: both operands should be on the stack here.
                il.Append(Instruction.Create(OpCodes.Pop));
                il.Append(Instruction.Create(OpCodes.Pop));
                il.Append(Instruction.Create(OpCodes.Ldc_I4_0));
                return ExprResult.Int32(0);

            default:
                _diagnostics.Report(0, $"Unsupported expression '{e.GetType().Name}'.");
                il.Append(Instruction.Create(OpCodes.Ldc_I4_0));
                return ExprResult.Int32(0);
        }
    }

    private ExprKind ClassifyExpression(BoundExpression e)
    {
        switch (e)
        {
            case BoundIntegerLiteral:
                return ExprKind.Int32;
            case BoundVariableExpression:
                return ExprKind.QExpr;
            case BoundBinaryExpression b:
                var lk = ClassifyExpression(b.Left);
                var rk = ClassifyExpression(b.Right);
                if (lk == ExprKind.Int32 && rk == ExprKind.Int32)
                {
                    return ExprKind.Int32;
                }
                return ExprKind.QExpr;
            default:
                return ExprKind.Int32;
        }
    }

    private void EmitDeferredPrints(
        ModuleDefinition module,
        ILProcessor il,
        Dictionary<string, FieldDefinition> antivalFields,
        IReadOnlyList<BoundExpression> prints)
    {
        var printVar = typeof(ToGoRuntime).GetMethod(nameof(ToGoRuntime.Print), new[] { typeof(PositronicVariable<int>) })!;
        var printVarRef = module.ImportReference(printVar);

        var printInt = typeof(ToGoRuntime).GetMethod(nameof(ToGoRuntime.Print), new[] { typeof(int) })!;
        var printIntRef = module.ImportReference(printInt);

        var printQb = typeof(ToGoRuntime).GetMethod(nameof(ToGoRuntime.Print), new[] { typeof(QuantumSuperposition.QuantumSoup.QuBit<int>) })!;
        var printQbRef = module.ImportReference(printQb);

        var qexprType = typeof(PositronicVariable<int>.QExpr);
        var qexprToQuBit = qexprType.GetMethod(
            "op_Implicit",
            BindingFlags.Public | BindingFlags.Static,
            binder: null,
            types: new[] { qexprType },
            modifiers: null)!;
        var qexprToQuBitRef = module.ImportReference(qexprToQuBit);

        for (int i = 0; i < prints.Count; i++)
        {
            var p = prints[i];

            if (p is BoundVariableExpression v && antivalFields.TryGetValue(v.Variable.Name, out var field))
            {
                il.Append(Instruction.Create(OpCodes.Ldsfld, field));
                il.Append(Instruction.Create(OpCodes.Call, printVarRef));
                continue;
            }

            // General expression printing (best-effort for MVP)
            var kind = EmitExpression(module, il, antivalFields, p);
            if (kind.Kind == ExprKind.Int32)
            {
                il.Append(Instruction.Create(OpCodes.Call, printIntRef));
            }
            else
            {
                il.Append(Instruction.Create(OpCodes.Call, qexprToQuBitRef));
                il.Append(Instruction.Create(OpCodes.Call, printQbRef));
            }
        }
    }
}
