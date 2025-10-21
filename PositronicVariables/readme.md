# PositronicVariable (.NET Library)
A time-looping variable container for quantum misfits and deterministic dreamers.
[![NuGet](https://img.shields.io/nuget/v/PositronicVariables.svg)](https://www.nuget.org/packages/PositronicVariables)

---

`PositronicVariable<T>` lets your code simulate values that evolve over iterative timelines. Think of it as Schrödinger's variable: simultaneously filled with regret and potential. Now enhanced with automatic convergence, timeline journaling, and existential debugging capabilities.

> Not exactly time travel, but close enough to confuse your boss.

---

## Features

- **Temporal Journaling**: Variables remember past states better than you remember birthdays.
- **Automatic Convergence**: Simulates logic until variables settle down (therapy not included).
- **NeuralNodule**: Quantum-flavored neurons for when your code needs group therapy.
- **Time Reversal**: Runs logic backward, forward, and sideways (flux capacitor optional).
- **Seamless Integration**: Plays nicely with `QuBit<T>` from [QuantumSuperposition](https://www.nuget.org/packages/QuantumSuperposition).

---

## Getting Started

### Installation

Available from NuGet (already available in futures you've yet to experience).

```shell
dotnet add package PositronicVariables
```

---

## Quick Example

What happens if you create a logical loop with no stable resolution?


```csharp
internal static class Program
{
    [DontPanic]
    private static void Main()
    {
        var antival = PositronicVariable<int>.GetOrCreate("antival", -1);
        Console.WriteLine($"The antival is {antival}");
        var val = -1 * antival;
        Console.WriteLine($"The value is {val}");
        antival.State = val;
    }
}
```

### Output (after convergence)
```
The antival is any(-1, 1)
The value is any(1, -1)
```

You're trapped in a two-state paradox. Like a light switch held halfway by Schrödinger's indecision. This is why convergence matters - without it, you're just running in timeline circles until the compiler cries.

---

## Feynman Diagrams for Programmers
Let's visualize what just happened:

```
Time →
[initial guess] - val = -1 * antival -→ antival = val -→ [back in time]
       ↑_______________________________________________________|
```

We created a **cycle**. PositronicVariables evaluate by iterating this loop until the values settle. If they never settle? You get superpositions. Like emotional baggage, but for integers.

```csharp
[DontPanic]
internal static void Main()
{
    double a = 2.0;
    var guess = PositronicVariable<double>.GetOrCreate("guess", 1.5);

    Console.WriteLine($"sqrt({a}) ≈ {guess.ToValues().Last()}");

    double v = guess.ToValues().Last();
    guess.State = (v + a / v) / 2.0;
}
```

#### Output:
```
sqrt(2) ≈ 1.414213562
```

Yes, this is real. No, we didn't skip a step. The past just updated itself when we committed to the present.

#### Why `.ToValues().Last()`?

You're not just creating a variable.

You're summoning a cloud of possibilities - all the potential values that guess could take as the program recursively rewinds and replays itself.
Like a looping dream sequence where it tries different outcomes until it finds one that satisfies the logic across all iterations.
The final value (of the not empty list) is the one that successfully stabilized the timeline after it loops through all the possibilities.

---

### Advanced: Folding Time with Pascal's Triangle

##  Pascal's Triangle via Causal Iteration

This is not a program that calculates Pascal's Triangle. This is a program that remembers the future row of Pascal's Triangle and tries to convince the past to believe it.

```csharp
using PositronicVariables.Attributes;
using PositronicVariables.Variables;

internal static class Program
{
    // Target row length to stop iterating
    private const int TargetRowLength = 10;

    // Entry point into the timeline. Begins *after* knowing the result.
    // Notice there is no explicit for loop here.
    [DontPanic]
    private static void Main()
    {
        // This line doesn't just create a variable. It creates a causality loop - one that stabilizes only when it finds a version of itself that matches the logical rules that come after it.
        // "Hello, I'm row. I will become a version of Pascal's Triangle when I grow up. Here is my future self: [1, 3, 3, 1]. Please backfill everything I need to become that."

        var row = PositronicVariable<ComparableList>.GetOrCreate("row", new ComparableList(1));
        // Print the final result before we do the work.
        Console.WriteLine("Final converged row: " + row);

        // “Give me the version of this variable that finally stopped fighting itself. I'll use that.”
        // The value hasn't been computed yet - it's being repeatedly derived in time.
        // Choose the longest known row so we always advance.
        ComparableList currentRowWrapper = row.ToValues().OrderBy(v => v.Items.Count).Last();
        List<int> currentRow = currentRowWrapper.Items;

        if (currentRow.Count < TargetRowLength)
        {
            // We haven't reached the desired row length.
            // Compute the next row of Pascal's Triangle.
            List<int> nextRow = ComputeNextRow(currentRow);
            var nextComparable = new ComparableList(nextRow.ToArray());

            // Narrow (Required) instead of plain Assign so reverse half-cycles
            // append slices and the union grows across iterations.
            row.Required = PositronicVariable<ComparableList>.Project(row, _ => nextComparable);
            // (No explicit printing or looping here-the simulation will re-invoke Main() automatically.)
        }
        else
        {
            // A bit like in the 1960's movie "The Time Machine" when the time traveler pulls the lever when the world was ending.
            row.UnifyAll();
        }
    }

    /// <summary>
    /// Computes the next row of Pascal's Triangle given the current row.
    /// </summary>
    /// <param name="current">The current row as a List of int.</param>
    /// <returns>A new List of int representing the next row.</returns>
    private static List<int> ComputeNextRow(List<int> current)
    {
        // The next row always starts with 1.
        var next = new List<int> { 1 };

        // Each interior element is the sum of two adjacent numbers in the current row.
        for (int i = 0; i < current.Count - 1; i++)
        {
            next.Add(current[i] + current[i + 1]);
        }

        // The row always ends with 1.
        next.Add(1);
        return next;
    }
}

/// <summary>
/// We implement a custom IComparable<ComparableList>. Why? Because convergence requires comparison. If time  can't tell when a value has changed, it can't collapse reality into a stable state and you loop forever.
/// </summary>
public class ComparableList : IComparable<ComparableList>
{
    public List<int> Items { get; }

    public ComparableList(params int[] values)
    {
        Items = new List<int>(values);
    }

    /// <summary>
    /// This wrapper isn't just syntactic sugar. It's the contract that keeps reality from fracturing.
    /// </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    public int CompareTo(ComparableList other)
    {
        if (other == null) return 1;
        int countCompare = Items.Count.CompareTo(other.Items.Count);
        if (countCompare != 0) return countCompare;

        for (int i = 0; i < Items.Count; i++)
        {
            int cmp = Items[i].CompareTo(other.Items[i]);
            if (cmp != 0) return cmp;
        }
        return 0;
    }

    public override bool Equals(object obj)
    {
        return obj is ComparableList cl && CompareTo(cl) == 0;
    }

    public override int GetHashCode()
    {
        return Items.Aggregate(17, (acc, val) => acc * 31 + val);
    }

    public override string ToString()
    {
        // Format like "1 4 6 4 1"
        return string.Join(" ", Items);
    }
}


```
Pascal's Triangle via convergence is just the beginning.
Imagine calculating Fibonacci sequences, game states, or user interfaces where the output stabilizes through pure logical declaration.
This isn't programming.
This is state declaration with timeline pressure.

#### Output:

```
Final converged row: any(1 1, 1 2 1, 1 3 3 1, 1 4 6 4 1, 1 5 10 10 5 1, 1 6 15 20 15 6 1, 1 7 21 35 35 21 7 1, 1 8 28 56 70 56 28 8 1, 1 9 36 84 126 126 84 36 9 1)
```

---

## In Summary

- You can **print values before they're calculated.**
- You can create **logical time paradoxes.**
- You can use **Feynman logic graphs** to debug your feedback loops.
- You can solve classical problems like sqrt, primes, or Pascal's triangle via **backward causality**.
- This is no longer programming. This is wizardry in C# form.

And yes, it's probably too much power for your coworkers. You're welcome.


## How It Works (The Short Version)

When you mark your entry method with `[DontPanic]`, the library automatically:

1. **Runs** your logic silently through negative-time (reverse entropy mode).
2. **Detects** repeating patterns and decides when your variables have "converged."
3. **Settles** into a stable timeline, executing your logic once more with variables at peace.

---

## Operators

Variables pretending to be regular numbers:
```csharp
var v = PositronicVariable<int>.GetOrCreate("v", 1);
var x = v + 5;
var y = x % 3;
```

It's all syntactic sugar over quantum indecision.

---

## Neural Nodule: DIY Quantum Brainstorming

This pattern gives us a reusable, DI-friendly "computed positronic variable." It’s great for building little probabilistic/possibilistic circuits where each node:
- reads the (possibly multivalued) states of other nodes,
- emits a (possibly multivalued) state of its own.
- and plays nicely with the convergence engine and timelines.

```csharp
var x = PositronicVariable<int>.GetOrCreate("x", 0);
var y = PositronicVariable<int>.GetOrCreate("y", 1);

var node = new NeuralNodule<int>(inputs =>
{
    var sum = inputs.Sum();
    return new QuBit<int>(new[] { sum % 5, (sum + 1) % 5 });
});

node.Inputs.Add(x);
node.Inputs.Add(y);
node.Fire();

Console.WriteLine($"Result: {node.Output}");
```

Run an entire network until consensus:

```csharp
NeuralNodule<int>.ConvergeNetwork(nodeA, nodeB, nodeC);
```

---

## What Is This Useful For?

- Creating chaotic yet stable feedback loops
- Building declarative state systems
- Simulating neural networks with quantum uncertainty
- Philosophical debugging sessions
- Impressing precisely 2.5 people at parties

---

## Limitations

- Not thread-safe. These variables can't handle that kind of pressure.
- Types must implement `IComparable` (sorry, incomparables).
- Can accidentally summon infinite universes (so use responsibly).

---

## License

Unlicensed. Use it, break it, ship it, regret it.

---

## Questions or Paradoxes?

File an issue or collapse reality and start again.