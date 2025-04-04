# PositronicVariable (.NET Library)
A time-looping variable container for quantum misfits and deterministic dreamers.
[![NuGet](https://img.shields.io/nuget/v/PositronicVariables.svg)](https://www.nuget.org/packages/PositronicVariables)

---

`PositronicVariable<T>` lets your code simulate values that evolve over iterative timelines. It's like if Schrödinger had a daily planner. This system plays nice with `QuBit<T>` superpositions from [QuantumSuperposition](https://www.nuget.org/packages/QuantumSuperposition), and introduces runtime convergence via timeline journaling, looped simulations, and multiverse debugging.

> This isn’t time travel.  
> This is **time therapy** for your variables.

---

## Features

- Temporal Journaling: Variables store their history like over-sharers in a group chat.
- Automatic Convergence: Run code until state stabilization without writing a while loop.
- NeuralNodule: Build quantum-flavored logic gates using positronic variables.
- Time Reversal: Temporarily run logic in negative entropy mode to simulate parallel timelines.
- Seamless with QuBit<T>: Because pretending a variable can hold many states is *your thing now*.

---

## Getting Started

### Installation

Grab it from NuGet (coming soon, or possibly already available in a future you haven't met yet).

```
dotnet add package PositronicVariables
```

---

## Example: Time-Looping Variable

```csharp
private static PositronicVariable<int> antival;

private static void Main()
{
    // Clean slate
    PositronicRuntime.Instance.Reset();

    // Initial state
    antival = new PositronicVariable<int>(-1);

    // Loop until convergence
    PositronicVariable<int>.RunConvergenceLoop(MainLogic);
}

private static void MainLogic()
{
    Console.WriteLine($"The antival is {antival}");
    var val = (antival + 1) % 4;
    Console.WriteLine($"The value is {val}");
    antival.Assign(val);
}
```

### Output (after convergence)
```
The antival is any(0, 1, 2, 3)
The value is any(1, 2, 3, 0)
```

---

## How It Works

When you call `RunConvergenceLoop`, the library:

1. Silently runs your code in **negative time** (don’t worry, you don’t need a flux capacitor).
2. Builds a **timeline** for each variable using `QuBit<T>` snapshots.
3. Detects convergence by matching recent state cycles.
4. Unifies results and runs your code one last time in **forward time** — now stable.

---

## Operators

Yes, your variables can pretend to be integers:
```csharp
var v = new PositronicVariable<int>(1);
var x = v + 5;
var y = x % 3;
```

It’s all syntactic sugar for probabilistic anxiety

---

## Neural Nodule: DIY Brainstorming

```csharp
var x = new PositronicVariable<int>(0);
var y = new PositronicVariable<int>(1);

var node = new NeuralNodule<int>(inputs =>
{
    var sum = inputs.Sum();
    return new QuBit<int>(new[] { sum % 5, (sum + 1) % 5 });
});

node.Inputs.Add(x);
node.Inputs.Add(y);

// Fires the activation function and pushes result to Output
node.Fire();

Console.WriteLine($"Result: {node.Output}");
```

You can also run a full convergence loop on a network of nodes:
```csharp
NeuralNodule<int>.ConvergeNetwork(nodeA, nodeB, nodeC);
```

---

## API Highlights

```csharp
public class PositronicVariable<T>
{
    public void Assign(T value);
    public void Assign(PositronicVariable<T> other);
    public void CollapseToLastSlice();
    public static void RunConvergenceLoop(Action logic);
    public int Converged();
    public void UnifyAll();
    public QuBit<T> GetCurrentQBit();
    public IEnumerable<T> ToValues();
}
```

---

## What Is This Useful For?

- Simulating feedback loops in logic
- Declarative-style state propagation
- Neural graphs and causal networks
- Philosophical debugging
- Impressing very specific kinds of nerds

---

## Limitations

- Not thread-safe. These variables are *emotionally* unstable.
- Requires your types to be `struct, IComparable`.
- Can accidentally create infinite universes if you're not careful (just like real life).

---

## License

Unlicensed. Use it, break it, ship it, regret it.

---

## Questions or Paradoxes?

You can file an issue, or collapse your current state and try again.
