# PositronicVariables (.NET Library)

[![NuGet](https://img.shields.io/nuget/v/PositronicVariables.svg)](https://www.nuget.org/packages/PositronicVariables)

*A time-looping variable container for deterministic dreamers, recursive state machines, and integers with unfinished business.*

`PositronicVariables` is a .NET library for values that move the wrong way through execution.

Ordinary values travel forwards. They are calculated, assigned, passed along, and eventually forgotten by a runtime that has places to be.

A `PositronicVariable<T>` carries state backwards through the execution trace. It allows later assignments to revise earlier assumptions, then asks the programme to run the conversation again until the forwards-moving code and the backwards-moving variable agree on what must have happened.

When they cannot agree on one answer, the result may become a `QuBit<T>` superposition.

Not because the library has become vague.

Because the contradiction has become precise.

It is built on top of `QuBit<T>` from [QuantumSuperposition](https://www.nuget.org/packages/QuantumSuperposition), but it is not a quantum simulator. There are no physical qubits here. No gates. No Bloch spheres. No particles pretending to be well behaved.

This is a library for temporal logic, recursive state, causal feedback, transactional mutation, replay, convergence, and variables that occasionally need to negotiate with their own past.

> Not exactly time travel. More a strongly typed argument with causality.

---

## Installation

```shell
dotnet add package PositronicVariables
```

---

## The core mechanic: forwards code, backwards variables

The whole library rests on one strange idea:

**normal execution moves forwards, but positronic state propagates backwards.**

Most code works like this:

```text
Time ->
read value -> calculate result -> assign new value -> continue
```

A positronic variable disturbs that tidy little procession.

When you assign a value to a `PositronicVariable<T>`, that assignment is not treated as merely “what happens next”. It becomes evidence about what the variable must have been earlier for the execution to make sense.

So the engine replays.

It lets the ordinary code flow forwards again, while the positronic variable carries revised state backwards into the next pass. If the two directions meet cleanly, the system converges. If they meet in contradiction, the system preserves the contradiction as a structured set of possible states.

The loop is not a bug hidden inside the engine.

The loop is the point.

```text
Forward execution:
    antival -> val = -1 * antival -> antival.State = val

Backward positronic propagation:
    antival.State = val -> revise antival -> rerun forward expression
```

Together, those two directions form the causal knot:

```text
                ordinary execution
Time -> [antival] -> [val = -1 * antival] -> [antival.State = val]
          ^                                                |
          |                                                |
          +------------- positronic propagation ------------+
```

The programme walks around that knot until it finds a stable story.

Sometimes the story is a single value.

Sometimes it is a superposition.

Sometimes it is the programme quietly admitting that arithmetic and causality have been left unsupervised for too long.

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

Read the example in both directions.

Going forwards:

```text
antival starts as -1
val becomes -1 * antival
antival is assigned val
```

Going backwards:

```text
the final assignment tells antival what it must have been
the expression is replayed
the result revises the assignment
the assignment revises the original
```

If `antival` is `-1`, then `val` is `1`, so `antival` becomes `1`.

If `antival` is `1`, then `val` is `-1`, so `antival` becomes `-1`.

The loop does not settle on one integer. It alternates between two mutually dependent histories.

So the stable answer is not `-1`.

It is not `1`.

It is the two-state shape of the paradox:

```text
antival = any(-1, 1)
val     = any(1, -1)
```

This is not the engine failing to choose.

This is the engine refusing to falsify the timeline for your comfort.

---

## What this library is

A normal variable has a value.

A `PositronicVariable<T>` has a timeline.

That timeline may be revised, replayed, inspected, converged, snapshotted, restored, or recorded in an append-only ledger where every regrettable operation receives a timestamp and a place in the archive.

The library is useful when values depend on one another in loops:

* `a` depends on `b`;
* `b` depends on `c`;
* `c` has opinions about `a`;
* a later assignment changes what an earlier read must have meant;
* the system cannot move forward until the forwards and backwards passes agree.

Rather than pretending such structures are always mistakes, `PositronicVariables` gives them a place to happen deliberately.

Sometimes the loop stabilises.

Sometimes it produces a multi-state answer.

Sometimes it reveals that your model has summoned a paradox and expects you to provide biscuits.

---

## Core ideas

## 1. Positronic variables

`PositronicVariable<T>` stores values across a timeline rather than treating assignment as a single isolated event.

It can:

* hold ordinary values;
* hold `QuBit<T>` superpositions;
* track previous states;
* participate in convergence;
* propagate state backwards through replay;
* be updated through transactional workflows;
* export and restore snapshots;
* record operations through a ledger sink.

The point is not that every variable should behave this way.

The point is that some variables are born inside recursive families and should not be forced to pretend they live alone.

---

## 2. Convergence

Convergence is the process of repeatedly replaying the network until forwards execution and backwards propagation stop changing one another.

If the values settle, the system has found a fixed point.

If they do not settle into one value but produce a stable set of possibilities, the result may be a superposition.

If they continue misbehaving beyond the permitted limits, the engine has learned something useful: the model does not converge under the current rules.

That, too, is information.

```csharp
[DontPanic]
private static void Main()
{
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
}
```

A convergence pass is not a mystical event.

It is the engine asking the same question from both ends of time until the answers stop flinching.

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
[DontPanic]
private static void Main()
{
    var value = PositronicVariable<int>.GetOrCreate("value", 1);

    value.Assign(new QuBit<int>(new[] { 1, 2, 3 }));

    Console.WriteLine(value);
}
```

The result is not merely uncertain.

It is typed uncertainty with a forwarding address and a return ticket.

---

## Transactions and telemetry

The library includes STM-style transactional updates for coordinating multi-variable mutation.

Use the ambient transaction scope when several variables need to be read and written atomically. Read-only transactions can validate without taking locks. Write transactions stage changes and apply them during commit.

```csharp
using PositronicVariables.Transactions;

[DontPanic]
private static void Main()
{
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
}
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

[DontPanic]
private static void Main()
{
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
}
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

A positronic programme is easiest to picture as two arrows disagreeing politely about the order of events.

```text
Forward time:
    initial guess -> derived value -> assignment -> programme continues

Backward state:
    assignment -> revised variable -> replay -> new derived value
```

The antival example becomes:

```text
Time ->
[antival = -1] -> [val = -1 * antival] -> [antival.State = val]
       ^                                                  |
       |                                                  |
       +------------- revised positronic state -----------+
```

The loop is not inferred after the fact.

It is the execution model.

The forward pass creates the expression. The backward pass revises the premise. The next forward pass recomputes the expression using the revised premise. Around it goes until the timeline either settles or becomes a superposition of its own possible histories.

This is why the paradox example produces:

```text
antival = any(-1, 1)
val     = any(1, -1)
```

The result is the smallest stable shape that contains both directions of the loop.

A classical variable would pick a side.

A positronic variable keeps the receipts.

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

* Positronic variables that propagate state backwards through execution
* Ordinary forward execution meeting backwards timeline revision
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

> What if a value could carry its future backwards until its past made sense?

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
