# PositronicVariable (.NET Library)
A time looping variable container for quantum misfits and deterministic dreamers.
[![NuGet](https://img.shields.io/nuget/v/PositronicVariables.svg)](https://www.nuget.org/packages/PositronicVariables)

`PositronicVariable<T>` simulates values that evolve across iterative timelines. Think Schrödinger's variable: simultaneously filled with regret and potential. Now with automatic convergence, timeline journaling and existential debugging.

> Not exactly time travel but close enough to confuse your boss.

## Features
- Temporal journaling: remembers past states better than you remember birthdays
- Automatic convergence: simulates logic until values settle (therapy not included)
- NeuralNodule: quantum flavoured neurons for group therapy style computation
- Time reversal: runs logic backward, forward and sideways (flux capacitor optional)
- Seamless integration with `QuBit<T>` from [QuantumSuperposition](https://www.nuget.org/packages/QuantumSuperposition)

## Getting Started
### Installation
```shell
dotnet add package PositronicVariables
```

## Quick Example
Logical loop with no stable resolution:
```csharp
[DontPanic]
private static void Main()
{
    var antival = PositronicVariable<int>.GetOrCreate("antival", -1);
    Console.WriteLine($"The antival is {antival}");
    var val = -1 * antival;
    Console.WriteLine($"The value is {val}");
    antival.State = val;
}
```
### Output (after convergence)
```
The antival is any(-1, 1)
The value is any(1, -1)
```
Two state paradox. Like a light switch held halfway by indecision. Convergence matters. Without it you loop until the compiler cries.

## Feynman Diagram Style View
```
Time ->
[initial guess] - val = -1 * antival -> antival = val -> [back in time]
       ^_______________________________________________|
```
We created a cycle. The engine iterates until the values stabilise. If they never settle you get superpositions. Emotional baggage for integers.
```csharp
[DontPanic]
internal static void Main()
{
    double a = 2.0;
    var guess = PositronicVariable<double>.GetOrCreate("guess", 1.5);
    Console.WriteLine($"sqrt({a}) ≈ {guess.ToValues().Last()}");
    double v = guess.ToValues().Last();
    guess.State = (v + a / v) / 2.0; // Newton refinement
}
```
#### Output:
```
sqrt(2) ≈ 1.414213562
```
Past updated when present committed.

### Why `.ToValues().Last()`?
You summon a cloud of possibilities. The engine rewinds and replays trying variants until one satisfies all logical constraints. The last value of the non empty list is the converged timeline representative.

### Advanced: Folding Time With Pascal's Triangle
Not a program that calculates Pascal's Triangle. A program that remembers a future row and convinces the past to believe it.
```csharp
[DontPanic]
private static void Main()
{
    const int TargetRowLength = 10;
    var row = PositronicVariable<ComparableList>.GetOrCreate("row", new ComparableList(1));
    Console.WriteLine("Final converged row: " + row);
    var currentRowWrapper = row.ToValues().OrderBy(v => v.Items.Count).Last();
    var currentRow = currentRowWrapper.Items;
    if (currentRow.Count < TargetRowLength)
    {
        var nextRow = ComputeNextRow(currentRow);
        var nextComparable = new ComparableList(nextRow.ToArray());
        row.Required = PositronicVariable<ComparableList>.Project(row, _ => nextComparable);
    }
    else
    {
        row.UnifyAll();
    }
}
// Helpers omitted for brevity
```
Pascal via convergence is just the beginning. Fibonacci, game states, UI trees stabilising by declaration.

#### Output:
```
Final converged row: any(1 1, 1 2 1, 1 3 3 1, 1 4 6 4 1, 1 5 10 10 5 1, 1 6 15 20 15 6 1, 1 7 21 35 35 21 7 1, 1 8 28 56 70 56 28 8 1, 1 9 36 84 126 126 84 36 9 1)
```

## In Summary
- Print values before they are calculated
- Create logical time paradoxes
- Use Feynman style logic graphs to debug feedback loops
- Solve classical problems via backward causality
- Not just programming. Wizardry in C# form

Probably too much power for your co workers. You are welcome.

## How It Works (Short Version)
`[DontPanic]` entry method triggers:
1. Runs logic silently through negative time (reverse entropy mode)
2. Detects repeating patterns and decides convergence
3. Replays once more with stable values

## Operators
Variables pretending to be regular numbers:
```csharp
var v = PositronicVariable<int>.GetOrCreate("v", 1);
var x = v + 5;
var y = x % 3;
```
Syntactic sugar over quantum indecision.

## Neural Nodule: DIY Quantum Brainstorming
Reusable computed positronic variable pattern. Build probabilistic circuits: nodes read multivalued inputs and emit multivalued outputs and cooperate with convergence.
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
NeuralNodule<int>.ConvergeNetwork(node);
```
## Useful For
- Chaotic yet stable feedback loops
- Declarative state systems
- Probabilistic neural style networks
- Philosophical debugging sessions
- Impressing precisely 2.5 people at parties

## Limitations
- Not thread safe
- Types must implement `IComparable`
- Can accidentally summon infinite universes (use responsibly)

## License
Unlicensed. Use it. Break it. Ship it. Regret it.

## Questions or Paradoxes?
File an issue or collapse reality and start again.