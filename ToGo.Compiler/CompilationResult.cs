using System.Collections.Generic;
using ToGo.Compiler.Diagnostics;

namespace ToGo.Compiler;

public sealed record CompilationResult(
    bool Success,
    string? OutputPath,
    IReadOnlyList<Diagnostic> Diagnostics);
