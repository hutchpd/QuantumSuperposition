using System;
using PositronicVariables.Attributes;
using PositronicVariables.Variables;

using QuantumSuperposition.Core;

internal static class Program
{
    [DontPanic]
    private static void Main()
    {
        QuantumConfig.EnableNonObservationalArithmetic = true;
        QuantumConfig.EnableCommutativeCache = true;

        var a = PositronicVariable<int>.GetOrCreate("a", 5);
        Console.WriteLine($"a@t0 = {a}");

        var b = -a;
        Console.WriteLine($"b@t0 = {b}");

        var c = a + b + 3;
        Console.WriteLine($"c@t0 = {c}");

        a.Required = c;
    }
}