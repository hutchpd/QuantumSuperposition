# Persistence and Audit

`PositronicVariables` 1.9.0 adds two ways for a timeline to survive the process that produced it:

* timeline snapshots, for exporting and restoring `PositronicVariable<T>` state;
* file-backed ledger audit entries, for keeping an append-only record of operations.

One is for resuming the present.

The other is for interrogating the past.

Neither promises to make causality simpler. They merely ensure it leaves paperwork.

---

## Timeline snapshots

Use snapshots when you want to pause, inspect, move, or resume a variable timeline outside the runtime that created it.

A snapshot captures the visible timeline values of a `PositronicVariable<T>` and writes them to disk as JSON.

```csharp
var variable = PositronicVariable<int>.GetOrCreate("counter", 1, runtime);

variable.Assign(2);
variable.Assign(3);

variable.SaveSnapshot("Artifacts/Positronic/counter.json");

var restored = PositronicVariable<int>.LoadSnapshot(
    "Artifacts/Positronic/counter.json",
    runtime);
```

The restored variable receives a fresh runtime identity, because it is not the same object crawling back through the window. It is a reconstructed timeline with preserved contents.

The past has been copied, not resurrected.

---

## What a snapshot contains

Snapshot files contain:

* the source variable transaction id, for audit and debug correlation;
* the source transaction version;
* a capture timestamp;
* timeline slice values in order.

This is enough to restore the recorded timeline shape and inspect how the variable reached its saved state.

It is not a full metaphysical backup of every possible amplitude, intention, or regrettable decision that occurred along the way.

---

## Current snapshot scope

Snapshot persistence currently stores timeline values.

It does not store weighted amplitude metadata from the underlying `QuBit<T>` state.

That means snapshots are best understood as timeline persistence, not complete superposition persistence. If your variable has accumulated a beautifully weighted cloud of possibilities, the snapshot records the values in the timeline, not the full amplitude biography behind them.

Restored variables preserve:

* stored timeline contents;
* stored version number.

Restored variables receive:

* a fresh runtime identity.

This keeps restored state useful without pretending that a deserialised object is the original object wearing a false moustache.

---

## File-backed ledger audit

Use `FileLedgerSink` when you want a durable append-only record of timeline operations while leaving reverse replay behaviour unchanged.

```csharp
Ledger.Sink = new FileLedgerSink("Artifacts/Positronic/ledger.jsonl");
```

Once configured, ledger entries are written to a JSON Lines file.

Each line is a separate JSON object, suitable for later inspection, debugging, or quietly asking the system what it thought it was doing.

Each entry contains:

* `Timestamp`
* `CommitId`
* `OperationName`
* `OperationType`

The ledger is not there to fix the past.

It is there to make the past answerable.

---

## Clearing the ledger

`FileLedgerSink.Clear()` clears the active in-memory replay ledger only.

It does not truncate the audit file.

The on-disk file remains append-only, because an audit trail that forgets on command is less of an audit trail and more of a diary with legal anxiety.

This distinction matters:

* replay state can be cleared so the engine can continue cleanly;
* durable audit history remains on disk;
* external inspection still sees the full operation trail.

If you need to delete or rotate the audit file, do that explicitly as part of your own file management policy.

The library will not quietly shred history just because the present feels awkward.

---

## When to use which

Use a **timeline snapshot** when you want to:

* save a variable timeline;
* restore it later;
* move state between runs;
* inspect a timeline outside the original process;
* create reproducible examples from a known state.

Use a **file-backed ledger audit** when you want to:

* record operations durably;
* inspect commits after execution;
* debug timeline mutation;
* correlate runtime behaviour with transaction ids;
* preserve an append-only trace of what happened.

Snapshots answer:

> What did this variable look like when we captured it?

The ledger answers:

> What did the system do, and when did it claim to have done it?

Both are useful.

Neither should be trusted to make your model less strange.