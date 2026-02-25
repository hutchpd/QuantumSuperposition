namespace ToGo.Compiler.Diagnostics;

public readonly record struct Diagnostic(int Position, string Message)
{
    public override string ToString() => $"{Position}: {Message}";
}
