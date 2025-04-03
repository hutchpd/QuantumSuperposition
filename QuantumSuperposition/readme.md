
## QuantumSuperposition (.NET Library)
Parallel states, singular clarity.

QuantumSuperposition is a .NET library that brings a dash of quantum weirdness to your C# code. Inspired by the bizarre beauty of quantum mechanics, it lets your variables exist in multiple states simultaneously — just like Schrödinger’s cat, but with less moral ambiguity.

### Why Use QuantumSuperposition?
In quantum mechanics, superposition means a system can be in many states at once — until observed. In your code, this means:

- Want to check if a number is divisible by any value in a set?
- Need to assert that all values match a condition, without a loop forest?
- Want to write math expressions that magically apply to all possible inputs at once?

Congratulations. You want quantum variables. And now you can have them, without building a particle accelerator in your garage.

## Features

- Superposition Modes: Conjunctive (All) and Disjunctive (Any) states.  
- Arithmetic Ops: Use +, -, *, /, % on entire sets of possibilities.  
- Smart Comparisons: Logical ops like <, >=, == work across superpositions and scalars.  
- Eigenstates: Maintain original inputs even after transformation (yes, you’re basically a quantum historian).  
- State Filtering: Find what you want without lifting a foreach.  

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

###  QuBit<T> Enhancements
- **Weighted Superpositions**: QuBits can now carry probabilistic weight! Each state can be weighted, and arithmetic magically respects those weights.
- **Sampling Methods**:
  - `.SampleWeighted()` gives you a random outcome based on weight distribution (great for simulations, or indecision).
  - `.MostProbable()` returns the state with the highest chance of happening — much like your coffee spilling on your keyboard.
- **Equality & Hashing** are now *weight-aware*, so you can compare QuBits without triggering an existential crisis.
- **Implicit Cast to T**: Want to collapse a QuBit into a value without typing `.SampleWeighted()` like a peasant? Now you can just assign it and let the compiler do the work. ✨
- **`.WithWeights(...)` Functional Constructor**: Apply new weights to your existing multiverse without rewriting the whole thing. Just like therapy, but for code.

###  Eigenstates<T> Gets Fancy Too
- **Weighted Keys**: Same idea, but applied to key-value preservation. Now you can weight how much you believe each key deserves to exist.
- **TopNByWeight(n)**: Because sometimes you just want the best few parallel universes.
- **FilterByWeight(...)**: Drop the low-probability riff-raff.
- **CollapseWeighted() / SampleWeighted()**: Similar to QuBit, these collapse to the most likely or randomly chosen key.
- **Safe Arithmetic Expansion**: Instead of producing terrifying M×N state space blowups, we now **combine results** with merged weights. No infinite loops. No RAM meltdowns. You're welcome.
- **Weight-aware equality and GetHashCode()** so that equality comparisons no longer pretend the world is flat.

###  Performance Note
You *can* still go full Cartesian if you want, but we don’t do it for you because we respect your CPU. If you're feeling brave, build `QuBit<(A,B)>` yourself and join the fun in exponential land.


## Advanced Concepts

Superposition Modes  
- Disjunctive (Any) — “Any of these values might work.”  
- Conjunctive (All) — “They all better pass, or we riot.”

Arithmetic & Logic That Feels Like Sorcery  
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
