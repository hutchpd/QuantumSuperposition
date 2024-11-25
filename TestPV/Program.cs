using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;

using System;

internal static class Program
{
    private static void Main()
    {
        // Encapsulate the main logic in a method or delegate
        PositronicVariable<int>.RunConvergenceLoop(MainLogic);
    }

    private static void MainLogic()
    {
        // Initialize positronic variables
        var antival = new PositronicVariable<int>(-1);

        // Display initial state
        Console.WriteLine($"The antival is {antival}");

        // Manipulate the value in a "positronic" way
        var val = (antival + 1) % 3;

        Console.WriteLine($"The value is {val}");

        antival.Assign(val);
    }
}




//public class PositronicVariable<T> : IDisposable where T : struct
//{
//    private Stack<Action> operations = new Stack<Action>();
//    private Stack<string> outputBuffer = new Stack<string>();
//    private T value;
//    private List<T> history = new List<T>();
//    private bool isDisposed = false;

//    public PositronicVariable(T initialValue)
//    {
//        value = initialValue;
//        history.Add(initialValue);
//        Log($"Initialized with value: {initialValue}");
//    }

//    public T GetValue()
//    {
//        return value;
//    }

//    public void SetValue(T newValue)
//    {
//        operations.Push(() => value = history.Last()); // Push the reverse operation onto the stack
//        history.Add(value); // Save current value to history for possible reversal
//        value = newValue;
//        Log($"Value updated to {newValue}");
//    }

//    /// <summary>
//    /// 
//    /// </summary>
//    /// <param name="var"></param>
//    /// <param name="operand"></param>
//    /// <returns></returns>
//    public static PositronicVariable<T> operator +(PositronicVariable<T> var, T operand)
//    {
//        throw new NotImplementedException();
//    }

//    public static PositronicVariable<T> operator %(PositronicVariable<T> var, T operand)
//    {
//        var newValue = (dynamic)var.value % operand;
//        var.SetValue(newValue);
//        return var;
//    }

//    public void Dispose()
//    {
//        if (!isDisposed)
//        {
//            ReverseOperations();
//            PrintBufferedOutput();
//            isDisposed = true;
//        }
//    }

//    private void ReverseOperations()
//    {
//        while (operations.Count > 0)
//        {
//            var operation = operations.Pop();
//            operation.Invoke();
//        }
//    }

//    private void Log(string message)
//    {
//        outputBuffer.Push(message);
//    }

//    private void PrintBufferedOutput()
//    {
//        while (outputBuffer.Count > 0)
//        {
//            Console.WriteLine(outputBuffer.Pop());
//        }
//    }

//    // Simulation of converging timelines and superposition states (simplified)
//    public void FinalizeState()
//    {
//        if (history.Distinct().Count() > 1) // Check if states are diverging
//        {
//            Log("States are diverging, attempting to converge...");
//            var superposition = new Eigenstates<T>(history.Distinct());
//            Log($"Superposition state achieved: {string.Join(", ", superposition.ToValues())}");
//        }
//        else
//        {
//            Log("States have converged.");
//        }
//    }
//}

