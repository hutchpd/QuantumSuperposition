using System.Collections.Generic;

namespace ToGo.Compiler.Diagnostics;

public sealed class DiagnosticBag
{
    private readonly List<Diagnostic> _diagnostics = new();

    public IReadOnlyList<Diagnostic> Diagnostics => _diagnostics;

    public void Report(int position, string message)
    {
        _diagnostics.Add(new Diagnostic(position, message));
    }

    public void AddRange(IEnumerable<Diagnostic> diagnostics)
    {
        foreach (var d in diagnostics)
        {
            _diagnostics.Add(d);
        }
    }
}
