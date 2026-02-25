using System;
using System.IO;
using ToGo.Compiler.Binder;
using ToGo.Compiler.CodeGen;
using ToGo.Compiler.Diagnostics;
using ToGo.Compiler.Lowering;
using ToGo.Compiler.Parser;

namespace ToGo.Compiler;

public sealed class ToGoCompiler
{
    public CompilationResult CompileFile(string inputPath, string outputAssemblyPath)
    {
        ArgumentNullException.ThrowIfNull(inputPath);
        ArgumentNullException.ThrowIfNull(outputAssemblyPath);

        var diagnostics = new DiagnosticBag();

        string text;
        try
        {
            text = File.ReadAllText(inputPath);
        }
        catch (Exception ex)
        {
            diagnostics.Report(0, $"Failed to read '{inputPath}': {ex.Message}");
            return new CompilationResult(false, null, diagnostics.Diagnostics);
        }

        var parser = new ToGo.Compiler.Parser.Parser(text, diagnostics);
        var syntax = parser.ParseProgram();

        var binder = new Binder.Binder(diagnostics);
        var bound = binder.BindProgram(syntax);

        var lowerer = new Lowerer();
        var lowered = lowerer.Lower(bound);

        Directory.CreateDirectory(Path.GetDirectoryName(outputAssemblyPath) ?? ".");

        var codegen = new CecilCodeGenerator(diagnostics);
        codegen.Generate(lowered, outputAssemblyPath);

        if (diagnostics.Diagnostics.Count == 0)
        {
            WriteRuntimeConfig(outputAssemblyPath);
            CopyNearbyRuntimeAssemblies(outputAssemblyPath);
            return new CompilationResult(true, outputAssemblyPath, diagnostics.Diagnostics);
        }

        return new CompilationResult(false, null, diagnostics.Diagnostics);
    }

    private static void WriteRuntimeConfig(string outputAssemblyPath)
    {
        var dir = Path.GetDirectoryName(outputAssemblyPath) ?? ".";
        var name = Path.GetFileNameWithoutExtension(outputAssemblyPath);
        var runtimeConfigPath = Path.Combine(dir, name + ".runtimeconfig.json");

        // Minimal runtimeconfig to allow `dotnet <dll>`.
        const string json = "{\n  \"runtimeOptions\": {\n    \"tfm\": \"net8.0\",\n    \"framework\": {\n      \"name\": \"Microsoft.NETCore.App\",\n      \"version\": \"8.0.0\"\n    }\n  }\n}\n";
        File.WriteAllText(runtimeConfigPath, json);
    }

    private static void CopyNearbyRuntimeAssemblies(string outputAssemblyPath)
    {
        // MVP convenience: copy all dlls next to the compiler into the output folder so the
        // generated program can resolve project/package dependencies without generating a deps.json.
        var srcDir = AppContext.BaseDirectory;
        var dstDir = Path.GetDirectoryName(outputAssemblyPath) ?? ".";

        foreach (var dll in Directory.EnumerateFiles(srcDir, "*.dll"))
        {
            var fileName = Path.GetFileName(dll);
            var dst = Path.Combine(dstDir, fileName);

            if (string.Equals(Path.GetFullPath(dll), Path.GetFullPath(dst), StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            File.Copy(dll, dst, overwrite: true);
        }
    }
}
