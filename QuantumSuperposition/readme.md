## QuantumSuperposition (.NET Library)
[![NuGet](https://img.shields.io/nuget/v/QuantumSuperposition.svg)](https://www.nuget.org/packages/QuantumSuperposition)

.NET’s most confident way to say “maybe”

QuantumSuperposition is a .NET library that brings a dash of quantum weirdness to your C# code. Inspired by the bizarre beauty of quantum mechanics, it lets your variables exist in multiple states simultaneously — just like Schrödinger’s cat, but with less moral ambiguity.

### Why Use QuantumSuperposition?
In quantum mechanics, superposition means a system can be in many states at once — until observed. In your code, this means:

- Want to check if a number is divisible by any value in a set?
- Need to assert that all values match a condition, without a loop forest?
- Want to write math expressions that magically apply to all possible inputs at once?

Congratulations. You want quantum variables. And now you can have them, without building a particle accelerator in your garage.

## Features

- **Superposition Modes**: Conjunctive (All) and Disjunctive (Any) states.
- **Arithmetic Ops**: Use `+`, `-`, `*`, `/`, `%` on entire sets of possibilities.
- **Smart Comparisons**: Logical ops like `<`, `>=`, `==` work across superpositions and scalars.
- **Eigenstates**: Maintain original inputs even after transformation (yes, you’re basically a quantum historian).
- **State Filtering**: Find what you want without lifting a `foreach`.
- **Weighted Superpositions**: Attach complex amplitudes to your chaos.
- **Sampling**: Collapse deterministically, probabilistically, or mock it for tests and demo wizardry.
- **Entanglement Support**: Because what’s better than one indecisive variable? A whole clique of them, sharing their fate.
- **Basis Transforms**: Observe in the Hadamard basis (or invent your own) because reality is optional.
- **Collapse Replay / Versioning**: Deterministically re-watch the same quantum accident over and over like it's a sitcom.

## Core Quantum Behavior

Because behaving normally is for classical variables.

- **Superposition**: Your variable is now every possible version of itself — until someone looks. Schrödinger vibes fully engaged.
- **Probability Amplitudes**: Assign complex numbers to your indecision. It's not overengineering, it's *science*.
- **Amplitude Normalization**: Keeps your chaos unit-balanced. Otherwise the math police show up.
- **Observation & Collapse**: You peek, it breaks. Just like your dev environment.
- **Multi-Basis Sampling**: Observe in Hadamard, or invent your own bizarre dimension to peek from.
- **Collapse Effects**: Entangled variables react when one is observed. Think of it like drama between emotionally attached functions.
- **Collapse Replay**: Deterministically relive your mistakes using a fixed seed. Because debugging *is* time travel.
- **Collapse Mocking**: Force a specific outcome, for testing, demonstrations, or satisfying your inner control freak.

## Entanglement Mechanics

The weird just got weirder. QuBits can now share destiny in style:

- **Entangled Variable Linking**: Tie variables together like they're in a codependent relationship.
- **Collapse Propagation**: Observing one causes collapse across the entire group — because misery loves company.
- **Tensor Product Expansion**: Generate all state combos across multiple QuBits, like a quantum group project.
- **Entangled Group Mutation Propagation**: Mutate one, mutate them all. Drama ensues.
- **Entanglement Group Versioning**: Track generational history of entangled graphs, because even quantum relationships have baggage.
- **Entanglement Guardrails**: Blocks self-links, paradoxes, and other crimes against nature.
- **Multi-Party Collapse Agreement**: Observers agree on a shared reality. For once.
- **Entanglement Locking / Freezing**: Prevent changes during critical operations — useful when the multiverse needs a time-out.
- **Entanglement Group Tagging / Naming**: Name your entanglement groups like pets. Examples: `BellPair_A`, `QuantumDrama42`.
- **Partial Collapse Staging**: Observe one qubit now, another later. Suspense!
- **Entanglement Graph Diagnostics**: Inspect group sizes, circular references, and the chaos % — an actual metric we now regret naming.


## QuBit<T> Enhancements

- **Weighted Superpositions**: QuBits can now carry probabilistic weight! Each state can be weighted, and arithmetic magically respects those weights.
- **Sampling Methods**:
  - `.SampleWeighted()` gives you a random outcome based on weight distribution (great for simulations, or indecision).
  - `.MostProbable()` returns the state with the highest chance of happening — much like your coffee spilling on your keyboard.
- **Equality & Hashing** are now *weight-aware*, so you can compare QuBits without triggering an existential crisis.
- **Implicit Cast to T**: Want to collapse a QuBit into a value without typing `.SampleWeighted()` like a peasant? Now you can just assign it and let the compiler do the work. ✨
- **`.WithWeights(...)` Functional Constructor**: Apply new weights to your existing multiverse without rewriting the whole thing. Just like therapy, but for code.

## Eigenstates<T> Gets Fancy Too

- **Weighted Keys**: Same idea, but applied to key-value preservation. Now you can weight how much you believe each key deserves to exist.
- **TopNByWeight(n)**: Because sometimes you just want the best few parallel universes.
- **FilterByWeight(...)**: Drop the low-probability riff-raff.
- **CollapseWeighted() / SampleWeighted()**: Similar to QuBit, these collapse to the most likely or randomly chosen key.
- **Safe Arithmetic Expansion**: Instead of producing terrifying M×N state space blowups, we now **combine results** with merged weights. No infinite loops. No RAM meltdowns. You're welcome.
- **Weight-aware equality and GetHashCode()** so that equality comparisons no longer pretend the world is flat.

## Probabilistic & Functional Sorcery

Because collapsing reality should be optional. These features let you get freaky with logic and structure — *without* observation causing your fragile multiverse to unravel.

- **`p_op` – Conditional Without Collapse**: Choose branches based on conditions without collapsing the state. Schrödinger's choice logic.
- **`p_func` – Functional State Transforms**: Map, filter, flatten — all without collapsing. LINQ for the superposed soul.
- **Non-Observational Arithmetic**: Enable operations like `+`, `*`, etc., without collapsing your QuBit. You get the math, *and* you keep the quantum soup. Have your waveform and eat it too.
- **Weighted Function Composition**: Let probabilistic weights affect how branching logic plays out. Now your uncertainty has influence.
- **Commutative Optimization**: Cache results of pure, commutative operations. Why recompute 2+3 when 3+2 already suffered that fate?
- **Monad-Compatible Superpositions**: LINQ-style `.Select()`, `.Where()`, `.SelectMany()` with lazy evaluation — the cool kind of lazy that optimizes performance, not just vibes.

    ### Enabling Non-Observational Arithmetic (if you're into that kind of thing)

    ```csharp
    QuantumConfig.EnableNonObservationalArithmetic = true;
    ```

    This allows arithmetic to operate without forcing a collapse. We don't judge. It's your multiverse (as a default this is on, but you can turn it off if you want to be a purist).

## Getting Started

### Installation  
Via .NET CLI:
```
dotnet add package QuantumSuperposition
```
Or with NuGet Package Manager Console:
```
Install-Package QuantumSuperposition
```

## Usage Examples

### Prime Number Checking  
Want to find primes without making your code look like a cryptography dissertation?
```csharp
static bool IsPrime(int number)
{
    var divisors = new QuBit<int>(Enumerable.Range(2, number - 2));
    return (number % divisors).EvaluateAll();
    // True if number is indivisible by any in divisors → therefore prime
}

for (int i = 1; i <= 100; i++)
{
    if (IsPrime(i))
        Console.WriteLine($"{i} is prime!");
}
```

### Finding Factors  
You can treat divisors as states and filter by computed results:
```csharp
static Eigenstates<int> Factors(int number)
{
    var candidates = new Eigenstates<int>(Enumerable.Range(1, number), x => number % x);
    return candidates == 0; // Give me the ones that divide cleanly
}
```

### Minimum Value Calculation  
Think of this like a quantum game show where only the smallest contestant survives:
```csharp
static int MinValue(IEnumerable<int> numbers)
{
    var eigen = new Eigenstates<int>(numbers);
    var result = eigen.Any() <= eigen.All(); // anyone less than or equal to everyone
    return result.ToValues().First();
}
```

### Complex Number Arithmetic with QuBit and Eigenstates

#### Performing Arithmetic Operations on Complex QuBit States

```csharp
using System;
using System.Collections.Generic;
using System.Numerics;
using QuantumSuperposition;

public class ComplexArithmeticExample
{
    public static void Main()
    {
        var qubit = new QuBit<Complex>(
            new List<Complex> { new Complex(1, 2), new Complex(3, 4) },
            new ComplexOperators()
        );
        var scalar = new Complex(1, 1);
        var resultQuBit = qubit + scalar;

        Console.WriteLine("After addition:");
        foreach (var state in resultQuBit.States)
            Console.WriteLine(state);

        var qubitA = new QuBit<Complex>(
            new List<Complex> { new Complex(2, 0), new Complex(1, 1) },
            new ComplexOperators()
        );
        var qubitB = new QuBit<Complex>(
            new List<Complex> { new Complex(3, 0), new Complex(0, 2) },
            new ComplexOperators()
        );

        var multiplied = qubitA * qubitB;
        Console.WriteLine("After multiplication:");
        foreach (var state in multiplied.States)
            Console.WriteLine(state);
    }
}
```

#### Weighted Superpositions and Weight Normalization

```csharp
using System;
using System.Collections.Generic;
using System.Numerics;
using QuantumSuperposition;

public class WeightedQuBitExample
{
    public static void Main()
    {
        var weightedItems = new[]
        {
            (new Complex(1, 0), (Complex)0.5),
            (new Complex(2, 0), (Complex)1.0)
        };

        var qubit = new QuBit<Complex>(weightedItems, new ComplexOperators());
        qubit.Append(new Complex(1, 0));
        qubit.NormaliseWeights();

        Console.WriteLine("Weighted and normalized values:");
        foreach (var (value, weight) in qubit.ToWeightedValues())
            Console.WriteLine($"State: {value}, Weight: {weight}");
    }
}
```

### Functional & LINQ-Style Operations

#### Conditional Transformation without Collapse

```csharp
using System;
using QuantumSuperposition;

public class ConditionalTransformationExample
{
    public static void Main()
    {
        var qubit = new QuBit<int>(new[] { 1, 2, 3 });

        var transformed = qubit.Conditional(
            (value, weight) => value % 2 == 0,
            qb => qb.Select(x => x * 10),
            qb => qb.Select(x => x + 5)
        );

        Console.WriteLine("Transformed states:");
        foreach (var state in transformed.States)
            Console.WriteLine(state);
    }
}
```

#### Chaining LINQ Operations

```csharp
using System;
using QuantumSuperposition;

public class LinqChainExample
{
    public static void Main()
    {
        var qubit = new QuBit<int>(new[] { 1, 2 });

        var result = qubit.Select(x => x + 1)
                          .Where(x => x > 2)
                          .SelectMany(x => new QuBit<int>(new[] { x * 3, x * 4 }));

        Console.WriteLine("Final states after chained LINQ operations:");
        foreach (var state in result.States)
            Console.WriteLine(state);
    }
}
```

---

### Entanglement and Collapse Propagation

```csharp
using System;
using QuantumSuperposition;

public class EntanglementExample
{
    public static void Main()
    {
        var system = new QuantumSystem();
        var qubitA = new QuBit<int>(system, new[] { 0, 1 });
        var qubitB = new QuBit<int>(system, new[] { 2, 3 });

        system.Entangle("BellPair_A", qubitA, qubitB);

        var observedA = qubitA.Observe(42);
        Console.WriteLine($"Observed value from qubit A: {observedA}");

        var observedB = qubitB.GetObservedValue();
        Console.WriteLine($"Observed value from qubit B: {observedB}");
    }
}
```

### Tensor Product Expansion

```csharp
using System;
using System.Numerics;
using QuantumSuperposition;

public class TensorProductExample
{
    public static void Main()
    {
        var qubit1 = new QuBit<int>(new (int, Complex)[]
        {
            (0, new Complex(1,0)),
            (1, new Complex(1,0))
        });

        var qubit2 = new QuBit<int>(new (int, Complex)[]
        {
            (0, new Complex(2,0)),
            (1, new Complex(0,2))
        });

        var productDict = QuantumMathUtility<int>.TensorProduct(qubit1, qubit2);

        Console.WriteLine("Tensor product states and their amplitudes:");
        foreach (var kvp in productDict)
        {
            Console.WriteLine($"State: [{string.Join(",", kvp.Key)}], Amplitude: {kvp.Value}");
        }
    }
}
```

## Performance Note
You *can* still go full Cartesian if you want, but we don’t do it for you because we respect your CPU. If you're feeling brave, build `QuBit<(A,B)>` yourself and join the fun in exponential land.

## Advanced Concepts

### Superposition Modes
- Disjunctive (Any) — “Any of these values might work.”  
- Conjunctive (All) — “They all better pass, or we riot.”

### Arithmetic & Logic That Feels Like Sorcery
Math just works across your whole quantum cloud.  
No loops. No boilerplate. Just operations that make sense across many states.

## Contributing  
Bug spotted in the matrix?  
Submit an issue. Write a pull request. We’d love your brain on this.

## License  
This library is released under the Unlicense. That means it's free, unshackled, and yours to tinker with.

## Contact  
Questions, fan mail, obscure quantum jokes?  
support@findonsoftware.com

## Acknowledgements  
Inspired by Damian Conway’s Quantum::Superpositions Perl module — where variables have been spooky since before it was cool.

## QuantumSuperposition Logo
                 ~   ~     ~     ~   ~    
             ~    __Q__    ___     ~
            ~    /  |  \  / _ \   ~    ~
    ~       ~    |  |  | | |_| |       ~
         ~       \__|__/  \___/    ~
                QuantumSuperposition
           Collapse your state. Collapse your doubts.
