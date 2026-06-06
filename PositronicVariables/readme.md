# PositronicVariables (.NET Library)

[![NuGet](https://img.shields.io/nuget/v/PositronicVariables.svg)](https://www.nuget.org/packages/PositronicVariables)

*A time-looping variable container for deterministic dreamers, recursive state machines, and integers with unfinished business.*

`PositronicVariables` is a .NET library for values that evolve across iterative timelines until they converge, contradict themselves honestly, or become a superposition of the answers they could not choose between.

It is built on top of `QuBit<T>` from [QuantumSuperposition](https://www.nuget.org/packages/QuantumSuperposition), but it is not a quantum simulator. There are no physical qubits here. No gates. No Bloch spheres. No particles pretending to be well behaved.

Instead, this is a library for temporal logic, recursive state, causal feedback, transactional mutation, convergence, replay, and variables that occasionally need to negotiate with their own future.

> Not exactly time travel. More a strongly typed argument with causality.

---

## What this library is

A normal variable has a value.

A `PositronicVariable<T>` has a history.

That history may be revised, replayed, inspected, converged, snapshotted, restored, or recorded in an append-only ledger where every regrettable operation receives a timestamp and a place in the archive.

The library is useful when values depend on one another in loops:

* `a` depends on `b`;
* `b` depends on `c`;
* `c` has opinions about `a`;
* the system cannot move forward until everyone stops changing their mind.

Rather than pretending such structures are always mistakes, `PositronicVariables` gives them a place to happen deliberately.

Sometimes the loop stabilises.

Sometimes it produces a multi-state answer.

Sometimes it reveals that your model has summoned a paradox and expects you to provide biscuits.

---

## Installation

```shell
dotnet add package PositronicVariables
```

---

## Quick example: a small paradox

Here is a logical loop with no single stable resolution:

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

After convergence:

```text
The antival is any(-1, 1)
The value is any(1, -1)
```

The system is being asked to hold a value that must be the negative of itself once the loop completes.

There is no single integer that satisfies the story, so the variable becomes the shape of the contradiction: two possible values, both necessary, neither permitted to win outright.

This is not the engine failing to choose.

This is the engine refusing to lie.

---

## Core ideas

## 1. Positronic variables

`PositronicVariable<T>` stores values across a timeline rather than treating assignment as a single isolated event.

It can:

* hold ordinary values;
* hold `QuBit<T>` superpositions;
* track previous states;
* participate in convergence;
* be updated through transactional workflows;
* export and restore snapshots;
* record operations through a ledger sink.

The point is not that every variable should behave this way.

The point is that some variables are born into recursive families and should not be forced to pretend they live alone.

---

## 2. Convergence

Convergence is the process of repeatedly evaluating a network of dependent values until the system reaches a stable state.

If the values settle, the system has found a fixed point.

If they do not settle into one value but produce a stable set of possibilities, the result may be a superposition.

If they continue misbehaving beyond the permitted limits, the engine has learned something useful: the model does not converge under the current rules.

That, too, is information.

```csharp
var x = PositronicVariable<int>.GetOrCreate("x", 1);
var y = PositronicVariable<int>.GetOrCreate("y", 2);

var node = new NeuralNodule<int>(inputs =>
{
    var sum = inputs.Sum();

    return new QuBit<int>(new[]
    {
        sum % 5,
        (sum + 1) % 5
    });
});

node.Inputs.Add(x);
node.Inputs.Add(y);

NeuralNodule<int>.ConvergeNetwork(node);

Console.WriteLine($"Final Output: {node.Output}");
```

A convergence pass is not a mystical event.

It is just a structured argument between current state, derived state, and the increasingly weary coordinator trying to make them agree.

---

## 3. NeuralNodule

`NeuralNodule<T>` represents a computation node that takes inputs, produces output, and participates in convergence.

It is not a neural network in the fashionable sense.

It is closer to a small computational committee: it reads values, applies a function, emits a result, and may be asked to repeat the process until the network stops rearranging the furniture.

Useful for:

* feedback loops;
* recursive computation;
* stabilisation experiments;
* probabilistic state networks;
* systems where the final value depends on the path taken to reach it.

---

## 4. Integration with QuantumSuperposition

`PositronicVariables` uses `QuBit<T>` from `QuantumSuperposition`.

That means a positronic variable can hold more than one possible state, and those states can be transformed using the same superposition semantics as the lower-level library.

```csharp
var value = PositronicVariable<int>.GetOrCreate("value", 1);

value.Assign(new QuBit<int>(new[] { 1, 2, 3 }));

Console.WriteLine(value);
```

The result is not merely uncertain.

It is typed uncertainty with a forwarding address.

---

## Transactions and telemetry

The library includes STM-style transactional updates for coordinating multi-variable mutation.

Use the ambient transaction scope when several variables need to be read and written atomically. Read-only transactions can validate without taking locks. Write transactions stage changes and apply them during commit.

```csharp
using PositronicVariables.Transactions;

var a = PositronicVariable<int>.GetOrCreate("a", 1);

// Read-only fast path
using (var tx = TransactionScope.Begin())
{
    TransactionScope.RecordRead(a);
}

// Update with retry and telemetry
TransactionV2.RunWithRetry(tx =>
{
    tx.RecordRead(a);

    var next = a.GetCurrentQBit();

    tx.StageWrite(a, next + 1);
});

Console.WriteLine(STMTelemetry.GetReport());
```

Telemetry can report:

* commits;
* retries;
* aborts;
* validation failures;
* lock-hold timings;
* contention hotspots.

This is useful because concurrency bugs rarely announce themselves with dignity.

They prefer to arrive late, intermittent, and carrying a stack trace that implicates everyone.

---

## Snapshot persistence

A timeline can be exported to JSON and restored later in a fresh runtime.

```csharp
using Microsoft.Extensions.Hosting;
using PositronicVariables.DependencyInjection;
using PositronicVariables.Runtime;

var path = Path.Combine("Artifacts", "Positronic", "state.json");
var runtime = PositronicAmbient.Current;

var value = PositronicVariable<int>.GetOrCreate(
    "resume-demo",
    1,
    runtime);

value.Assign(2);
value.Assign(3);

value.SaveSnapshot(path);

var hostBuilder = Host
    .CreateDefaultBuilder()
    .ConfigureServices(services => services.AddPositronicRuntime());

PositronicAmbient.InitialiseWith(hostBuilder);

var restored = PositronicVariable<int>.LoadSnapshot(
    path,
    PositronicAmbient.Current);

Console.WriteLine(string.Join(", ", restored.ToValues()));
```

Snapshots are useful when you want to pause a timeline, inspect it, move it somewhere else, or prove to a future version of yourself that the past really did look like that.

See `docs/Persistence.md`.

---

## Ledger audit sink

For append-only operation traces, configure a file-backed ledger sink before running a convergence pass or transactional workload:

```csharp
Ledger.Sink = new FileLedgerSink("Artifacts/Positronic/ledger.jsonl");
```

The ledger records durable operation traces.

Not because the past can be fixed.

Because it can be cross-examined.

---

## Feynman-style view

A tiny cycle:

```text
Time ->
[initial guess] -> val = -1 * antival -> antival = val -> [re-evaluate]
       ^_________________________________________________________|
```

The engine walks the loop until the values stabilise or until the contradiction becomes explicit.

In the example above, the system cannot honestly reduce to a single value. So it produces a superposed answer.

This is what happens when arithmetic and causality are left in a room together without supervision.

---

## Thread-safety notes

The library separates ordinary transactional mutation from convergence-driven timeline mutation.

That distinction matters.

### Ordinary updates

Use `TransactionV2` and `TransactionScope` for multi-threaded updates.

* Read-only transactions use the fast path and avoid lock acquisition.
* Writes are staged.
* Commits apply staged writes atomically.
* Per-variable locks protect committed mutation.
* Telemetry records retries, validation failures, and contention.

Outside callers should treat timelines as read-only unless they are operating through the approved transactional paths.

The public API exposes `IReadOnlyList<QuBit<T>>` where appropriate because mutable timelines should not be handed out like party favours.

### Convergence and replay

The convergence loop, reverse replay, forward replay, timeline archival, and ledger coordination are serialised through a single `ConvergenceCoordinator`.

The coordinator owns the exclusive engine token.

That means convergence behaves like a very polite single-passenger queue. Everyone gets a turn. No one is allowed to grab causality by the lapels and run sideways through the archive.

### Mutation gates

Timelines change in two approved places:

1. during transactional apply at commit, for variables in the write set;
2. inside the convergence coordinator while it holds the engine token.

Debug builds complain if code tries to mutate timelines outside these gateways.

The complaint is intentional.

It is the library noticing someone has opened a side door in reality.

### Ledger etiquette

Ledger entries are buffered inside transactions and appended exactly once after commit.

Direct poking of the global stack is discouraged and internally serialised.

History may be strange, but it should not be double-booked.

### Async context

Ambient transaction context and operator logging suppression use `AsyncLocal`.

That means `await` should not quietly wander off with the invariants.

It has tried before.

---

## Features

* Temporal state tracking
* Iterative convergence
* Recursive dependency handling
* Feedback loop modelling
* `NeuralNodule<T>` computation nodes
* Superposition-backed state through `QuBit<T>`
* STM-style transactional updates
* Read-only transaction fast path
* Commit, retry, abort, validation, and contention telemetry
* Timeline snapshot export and import
* File-backed append-only ledger audit sink
* Debug checks for unsafe timeline mutation
* Async-safe ambient transaction context

---

## Useful for

`PositronicVariables` may be useful if you are exploring:

* recursive state systems;
* feedback loops;
* temporal modelling;
* declarative state convergence;
* paradox-tolerant logic;
* probabilistic or multi-state computation;
* experimental programming models;
* philosophical debugging sessions with receipts.

It is probably not the correct tool if you need:

* ordinary application state;
* simple mutable variables;
* a conventional rules engine;
* production workflow orchestration;
* software that looks away politely when causality starts making noises.

---

## Limitations

* The convergence engine is not intended for multi-threaded mutation while a convergence loop is running.
* Types must implement `IComparable`.
* Snapshot persistence captures timeline values, not weighted amplitude metadata.
* This is an experimental library and should be treated as a playground rather than a production dependency.
* Recursive systems can still fail to converge if their rules do not permit stability.
* If you create a paradox, the library may preserve it more accurately than you wanted.

---

## Relationship to QuantumSuperposition

`QuantumSuperposition` models possible values.

`PositronicVariables` models possible histories.

The first asks:

> What if a value could remain many things until observed?

The second asks:

> What if a value could keep revising itself until its timeline made sense?

They are separate libraries, but they share a suspicion that early certainty is often just impatience in formal wear.

---

## License

Unlicense.

Use it, modify it, fork it, learn from it, regret it, explain it badly at parties.

No warranty is provided.

---

## Questions or paradoxes?

Open an issue if you find:

* convergence failures;
* unsafe mutation paths;
* ledger inconsistencies;
* transaction bugs;
* documentation that makes the timeline worse;
* a paradox that deserves a better home.

If reality collapses while filing the issue, include the last observed value.
