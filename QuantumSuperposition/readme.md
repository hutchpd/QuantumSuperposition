## QuantumSuperposition (.NET Library)
[![NuGet](https://img.shields.io/nuget/v/QuantumSuperposition.svg)](https://www.nuget.org/packages/QuantumSuperposition)
![Quantum Algorithms Inside](https://img.shields.io/badge/quantum--algorithms-included-blueviolet)

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

## Quantum Logic Gates

- **`q_logic` - State Transforms** lets you apply gate-like transformations to states, like a quantum wizard casting spells on your variables.
- **Built-in Gate Examples: Root-NOT, Hadamard, etc.** - Because who doesn't want to play with quantum gates?
- **Gate Set Registration: Plug in custom gates easily** - Like adding a new spell to your grimoire.
- **Gate Inversion: Auto-generate or define inverse gates** - Because sometimes you need to undo your quantum shenanigans.
- **Gate Timing/Ordering Strategy: Queue or sequence logic gate application** - Like a quantum conductor orchestrating your variables.
- **Quantum Gate Composition API: Allow X.Then(H).Then(CNOT) patterns** - Because chaining is the new black.
- **Parametric Gates: Define gates like RX(angle: π/4) for QAOA, QFT, etc.** - Because who doesn't love a good parameterized gate?
- **Gate Scheduling Visualizer: Generate ASCII/graph diagrams from gate sequences** - Because sometimes you need to see your quantum chaos in a pretty format.

## Quantum Algorithms

- **Built-in Quantum Algorithms**: Run real QFT and Grover’s Search logic on `QuantumSystem` like a true quantum dev — no PhD required.
- **Quantum Fourier Transform (QFT)**: A core building block in many quantum algorithms. It reveals periodicity in a quantum state, turning time-based signals into frequency — like a quantum DJ mixing up the basis.
- **Grover’s Search Algorithm**: A quantum algorithm for searching unsorted databases with quadratic speedup. It’s like having a quantum search engine that actually works.

Each algorithm internally schedules gate operations, which you can inspect, visualize, or export using the gate queue.

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

### Required Namespaces

For most usage, you'll want:

```csharp
using QuantumSuperposition.Core;
using QuantumSuperposition.QuantumSoup;
using QuantumSuperposition.Operators;
```

## Documentation

- [Getting Started](#getting-started)
- [Usage Examples](https://github.com/hutchpd/QuantumSuperposition/blob/master/QuantumSuperposition/docs/UsageExamples.md)
- [Entanglement & Collapse Propagation](https://github.com/hutchpd/QuantumSuperposition/blob/master/QuantumSuperposition/docs/Entanglement.md)
- [Functional & LINQ Operations](https://github.com/hutchpd/QuantumSuperposition/blob/master/QuantumSuperposition/docs/FunctionalOps.md)
- [Working with Complex Numbers](https://github.com/hutchpd/QuantumSuperposition/blob/master/QuantumSuperposition/docs/ComplexSupport.md)
- [Quantum Algorithms](https://github.com/hutchpd/QuantumSuperposition/blob/master/QuantumSuperposition/docs/QuantumAlgorithms.md)

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
