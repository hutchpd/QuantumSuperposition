# Positronic .NET Playground  
**QuantumSuperposition** + **PositronicVariables**  
_AKA: Two libraries walk into a multiverse and forget which one was real._

---

This repository contains two interlinked .NET libraries for working with uncertainty, multistate computation, and time-looped logic in elegant, brain-melting ways.

> Think of it as functional programming, but with way more existential doubt.

---

## 🌌 Included Libraries

### 🧠 QuantumSuperposition

> “Maybe” as a first-class value.

A library that allows .NET variables to hold multiple values at once — like Schrödinger’s cat, but without the vet bills.

- Define `QuBit<T>` superpositions: sets of possible values with optional weights.
- Perform arithmetic, comparisons, and logic across all values.
- Collapse to a specific value or sample based on weights.
- Clean APIs for set operations, filtering, and observation.
- Optional probabilistic behavior with amplitude weighting.

📖 [Full README](QuantumSuperposition/README.md)

---

### 🕒 PositronicVariables

> Temporal state machines with quantum journaling.

A framework built atop `QuBit<T>` that enables variables to evolve across simulated timelines. Useful for:

- Looping feedback networks
- Converging neural nodes
- Simulating systems until stabilized
- Hitting *Undo* on reality and trying again

- Automatically detects cycles across timelines.
- Merges diverging states into unified multiverses.
- Lets you simulate “what if” before committing to a result.
- `RunConvergenceLoop` executes your logic until everyone agrees. It’s like couple’s therapy, but for variables.

📖 [Full README](PositronicVariables/README.md)

---

## 🧪 Example Use Case

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

// Fires and mutates across time
NeuralNodule<int>.ConvergeNetwork(node);

Console.WriteLine($"Final Output: {node.Output}");
```

---

## 🛠 Development

- C# 10 / .NET 6+
- NuGet-ready projects (soon™)
- Tests included for quantum math and timeline logic

---

## 🧘 Philosophy

This project was born out of a deeply unhealthy desire to simulate philosophical paradoxes in enterprise-grade C#. Here’s what we believe:

- Uncertainty is not a bug — it’s a feature.
- A variable should be allowed to have an identity crisis.
- Time should be a reversible concept in your runtime.
- Your code deserves to collapse its own quantum wavefunction.

---

## 🧑‍💻 Contributing

Pull requests are welcome. Just remember: every change you make spawns another universe.

Open an issue if:

- The quantum logic doesn't behave
- The temporal recursion loops forever
- You accidentally converged your boss into a `null`

---

## 🪪 License

This code is released under the [Unlicense](https://unlicense.org/).  
Do whatever. Just don’t blame us when your compiler becomes sentient.

---

## 🙃 Final Thoughts

This repo is for people who:

- Think recursion is a lifestyle.
- Are disappointed that LINQ doesn't support parallel universes.
- Yell “collapse!” at their variables and expect them to listen.

Congratulations. You’re one of us now.
