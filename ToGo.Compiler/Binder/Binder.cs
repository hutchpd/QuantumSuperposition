using System;
using System.Collections.Generic;
using ToGo.Compiler.AST;
using ToGo.Compiler.Diagnostics;
using ToGo.Compiler.Lexer;

namespace ToGo.Compiler.Binder;

public sealed class Binder
{
    private readonly DiagnosticBag _diagnostics;
    private readonly Dictionary<string, VariableSymbol> _globals = new(StringComparer.Ordinal);

    public Binder(DiagnosticBag diagnostics)
    {
        _diagnostics = diagnostics ?? throw new ArgumentNullException(nameof(diagnostics));
    }

    public BoundProgram BindProgram(ProgramNode program)
    {
        var boundStatements = new List<BoundStatement>(program.Statements.Count);

        for (int i = 0; i < program.Statements.Count; i++)
        {
            var stmt = program.Statements[i];
            boundStatements.Add(BindTopLevelStatement(stmt));
        }

        return new BoundProgram(boundStatements);
    }

    private BoundStatement BindTopLevelStatement(StatementNode stmt)
    {
        switch (stmt)
        {
            case AntivalDeclarationNode d:
                return BindAntivalDeclaration(d);
            case TimeBlockNode t:
                return BindTimeBlock(t);
            default:
                _diagnostics.Report(0, $"Unsupported top-level statement '{stmt.GetType().Name}'.");
                return new BoundTimeBlock(Array.Empty<BoundStatement>());
        }
    }

    private BoundAntivalDeclaration BindAntivalDeclaration(AntivalDeclarationNode d)
    {
        if (_globals.ContainsKey(d.Name))
        {
            _diagnostics.Report(0, $"Duplicate antival declaration '{d.Name}'.");
            return new BoundAntivalDeclaration(_globals[d.Name]);
        }

        var sym = new VariableSymbol(d.Name);
        _globals.Add(d.Name, sym);
        return new BoundAntivalDeclaration(sym);
    }

    private BoundTimeBlock BindTimeBlock(TimeBlockNode t)
    {
        var stmts = new List<BoundStatement>(t.Statements.Count);
        for (int i = 0; i < t.Statements.Count; i++)
        {
            var s = t.Statements[i];
            stmts.Add(BindTimeStatement(s));
        }
        return new BoundTimeBlock(stmts);
    }

    private BoundStatement BindTimeStatement(StatementNode s)
    {
        switch (s)
        {
            case PrintStatementNode p:
                return new BoundPrintStatement(BindExpression(p.Expression));
            case AssignmentStatementNode a:
                return BindAssignment(a);
            default:
                _diagnostics.Report(0, $"Unsupported statement in time block '{s.GetType().Name}'.");
                return new BoundPrintStatement(new BoundIntegerLiteral(0));
        }
    }

    private BoundAssignmentStatement BindAssignment(AssignmentStatementNode a)
    {
        if (!_globals.TryGetValue(a.Name, out var sym))
        {
            _diagnostics.Report(0, $"Unknown antival '{a.Name}'.");
            sym = new VariableSymbol(a.Name);
        }

        var expr = BindExpression(a.Expression);
        return new BoundAssignmentStatement(sym, expr);
    }

    private BoundExpression BindExpression(ExpressionNode e)
    {
        switch (e)
        {
            case IntegerLiteralNode i:
                return new BoundIntegerLiteral(i.Value);
            case IdentifierExpressionNode id:
                if (!_globals.TryGetValue(id.Name, out var sym))
                {
                    _diagnostics.Report(0, $"Unknown identifier '{id.Name}'.");
                    sym = new VariableSymbol(id.Name);
                }
                return new BoundVariableExpression(sym);
            case BinaryExpressionNode b:
                var left = BindExpression(b.Left);
                var right = BindExpression(b.Right);
                if (b.OperatorKind != TokenKind.Plus)
                {
                    _diagnostics.Report(0, $"Unsupported binary operator '{b.OperatorKind}'.");
                }
                return new BoundBinaryExpression(left, b.OperatorKind, right);
            default:
                _diagnostics.Report(0, $"Unsupported expression '{e.GetType().Name}'.");
                return new BoundIntegerLiteral(0);
        }
    }
}
