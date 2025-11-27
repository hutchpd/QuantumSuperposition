# PositronicVariable (.NET Library)
A time looping variable container for quantum misfits and deterministic dreamers.
[![NuGet](https://img.shields.io/nuget/v/PositronicVariables.svg)](https://www.nuget.org/packages/PositronicVariables)

`PositronicVariable<T>` simulates values that evolve across iterative timelines. Think Schrödinger's variable: simultaneously filled with regret and potential. Now with automatic convergence, STM-backed transactional updates, telemetry, and existential debugging.

> Not exactly time travel but close enough to confuse your boss.

## Features
- Temporal journaling: remembers past states better than you remember birthdays
- Automatic convergence: simulates logic until values settle (therapy not included)
- NeuralNodule: quantum flavoured neurons for group therapy style computation
- Time reversal: runs logic backward, forward and sideways (flux capacitor optional)
- Seamless integration with `QuBit<T>` from [QuantumSuperposition](https://www.nuget.org/packages/QuantumSuperposition)
- STM telemetry: commits, retries, aborts, validation failures, lock-hold timings, contention hotspots
- Read-only fast path: validate-only transactions with no lock acquisition

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

## Transactions and Telemetry
Use the ambient transaction scope for multi-variable atomic updates. Read-only transactions validate without taking locks.
```csharp
using PositronicVariables.Transactions;

// Read-only fast path
a = PositronicVariable<int>.GetOrCreate("a", 1);
using (var tx = TransactionScope.Begin())
{
    TransactionScope.RecordRead(a);
}

// Update with retry and telemetry
TransactionV2.RunWithRetry(tx =>
{
    tx.RecordRead(a);
    var next = a.GetCurrentQBit();
    tx.StageWrite(a, next + 1); // stage a new qubit state
});

Console.WriteLine(STMTelemetry.GetReport());
```

## Feynman Diagram Style View
```
Time ->
[initial guess] - val = -1 * antival -> antival = val -> [back in time]
       ^_______________________________________________|
```
We created a cycle. The engine iterates until the values stabilise. If they never settle you get superpositions. Emotional baggage for integers.

## Thread-safety Notes
- Per-variable locks and versions guard non-transactional writes.
- Transactions acquire per-variable locks in id order to prevent deadlocks; read-only transactions use a fast path without locks.
- For concurrent writers on multiple variables, prefer `TransactionScope`/`TransactionV2` to ensure atomicity and serializability.
- The convergence loop itself is not designed for parallel mutation from multiple threads; run the loop on a single coordinator thread.

## Useful For
- Chaotic yet stable feedback loops
- Declarative state systems
- Probabilistic neural style networks
- Philosophical debugging sessions
- Impressing precisely 2.5 people at parties

## Limitations
- Convergence engine is not intended for multi-threaded mutation while the loop is running.
- Types must implement `IComparable`.

## License
Unlicensed. Use it. Break it. Ship it. Regret it.

## Questions or Paradoxes?
File an issue or collapse reality and start again.