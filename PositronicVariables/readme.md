# PositronicVariable (.NET Library)
A time-looping variable container for quantum misfits and deterministic dreamers.
[![NuGet](https://img.shields.io/nuget/v/PositronicVariables.svg)](https://www.nuget.org/packages/PositronicVariables)

---

`PositronicVariable<T>` lets your code simulate values that evolve over iterative timelines. Think of it as Schrödinger’s variable: simultaneously filled with regret and potential. Now enhanced with automatic convergence, timeline journaling, and existential debugging capabilities.

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

Here's a quick example of a Positronic integer variable that converges to a stable cycle:

```csharp
internal static class Program
{
    [PositronicEntry]
    private static void Main()
    {
        var antival = PositronicVariable<int>.GetOrCreate("antival", -1);
        Console.WriteLine($"The antival is {antival}");
        var val = -1 * antival;
        Console.WriteLine($"The value is {val}");
        antival.Assign(val);
    }
}
```

### Output (after convergence)
```
The antival is any(-1, 1)
The value is any(1, -1)
```

---

## String Example (for the literate variables)

Now you can quantum-entangle your strings too:

```csharp
internal static class Program
{
    [PositronicEntry]
    private static void Main()
    {
        var greeting = PositronicVariableRef<string>.GetOrCreate("greeting", "Hello");
        Console.WriteLine($"Current greeting: {greeting}");
        
        var nextGreeting = greeting.ToValues().First() switch
        {
            "Hello" => "Hi",
            "Hi" => "Hey",
            "Hey" => "Hello",
            _ => "Hello"
        };

        greeting.Assign(nextGreeting);
    }
}
```

### Output (after convergence)
```
Current greeting: any("Hello", "Hi", "Hey")
```

---

## How It Works (The Short Version)

When you mark your entry method with `[PositronicEntry]`, the library automatically:

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

It’s all syntactic sugar over quantum indecision.

---

## Neural Nodule: DIY Quantum Brainstorming

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