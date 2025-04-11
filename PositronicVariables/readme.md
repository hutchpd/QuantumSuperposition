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

What happens if you create a logical loop with no stable resolution?


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

You're trapped in a two-state paradox. Like a light switch held halfway by Schrödinger’s indecision. This is why convergence matters — without it, you're just running in timeline circles until the compiler cries.

---

## Feynman Diagrams for Programmers
Let’s visualize what just happened:

```
Time →
[initial guess] — val = -1 * antival —→ antival = val —→ [back in time]
       ↑_______________________________________________________|
```

We created a **cycle**. PositronicVariables evaluate by iterating this loop until the values settle. If they never settle? You get superpositions. Like emotional baggage, but for integers.

```csharp
[PositronicEntry]
static void Main()
{
    double a = 2.0;
    var guess = PositronicVariable<double>.GetOrCreate("guess", 1.5);

    // Watch the convergence happen before your eyes
    Console.WriteLine($"sqrt({a}) ≈ {guess.ToValues().Last()}");

    double v = guess.ToValues().Last();
    guess.Assign((v + a / v) / 2.0);
}
```

#### Output:
```
sqrt(2) ≈ 1.414213562
```

Yes, this is real. No, we didn’t skip a step. The past just updated itself when we committed to the present.

#### Why `.ToValues().Last()`?

You're not just creating a variable.

You’re summoning a cloud of possibilities — all the potential values that guess could take as the program recursively rewinds and replays itself.
Like a looping dream sequence where it tries different outcomes until it finds one that satisfies the logic across all iterations.
The final value is the one that successfully stabilized the timeline.

---

###  **Too Far? Let’s Fold the Universe for Primes**

##  Prime Discovery via Causal Iteration

This example discovers prime numbers not by checking every number, but by defining a single `candidate` variable that evolves forward through convergent timeline rewriting:

```csharp
[PositronicEntry]
internal static class Program
{
    private static void Main()
    {
        var candidate = PositronicVariable<int>.GetOrCreate("candidate", 2);
        var primes = new List<int>();

        for (int i = 2; i < 100; i++)
        {
            int number = candidate.ToValues().Last();

            if (IsPrime(number, primes))
            {
                primes.Add(number);
                Console.WriteLine(number);
            }

            candidate.Assign(number + 1);
        }
    }

    private static bool IsPrime(int n, List<int> primes)
    {
        if (n < 2) return false;

        foreach (var p in primes)
            if (n % p == 0) return false;

        return true;
    }
}
```

### What’s Happening?

- `candidate.ToValues().Last()` gives you the current *most accurate* prediction of the prime.
- `candidate.Assign(number + 1)` defines how that candidate moves forward through time.
- Each iteration *rewrites the past*, stabilizing around valid primes.
- No manual filtering or brute-force search. Just **temporal state progression**.

This is a state machine **without explicit states** — just a variable inching forward through causally consistent branches until the timeline locks in the truth.

#### Output:

```
2
3
5
7
11
13
...
97
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

## 🧠 In Summary

- You can **print values before they're calculated.**
- You can create **logical time paradoxes.**
- You can use **Feynman logic graphs** to debug your feedback loops.
- You can solve classical problems like sqrt, primes, or Pascal’s triangle via **backward causality**.
- This is no longer programming. This is wizardry in C# form.

And yes, it’s probably too much power for your coworkers. You're welcome.


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