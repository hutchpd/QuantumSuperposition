# Persistence and Audit

PositronicVariables 1.9.0 adds two persistence-oriented building blocks:

- Timeline snapshots for exporting and restoring `PositronicVariable<T>` state.
- File-backed ledger audit entries for durable append-only operation traces.

## Timeline snapshots

Use snapshots when you want to pause, inspect, or resume a variable timeline outside the process that created it.

```csharp
var variable = PositronicVariable<int>.GetOrCreate("counter", 1, runtime);
variable.Assign(2);
variable.Assign(3);

variable.SaveSnapshot("Artifacts/Positronic/counter.json");

var restored = PositronicVariable<int>.LoadSnapshot(
    "Artifacts/Positronic/counter.json",
    runtime);
```

Snapshot files contain:

- The source variable transaction id for audit/debug correlation
- The source transaction version
- A capture timestamp
- Timeline slice values in order

Current scope:

- Snapshot persistence stores timeline values, not weighted amplitude metadata.
- Restored variables receive a fresh runtime identity while preserving the stored version number and timeline contents.

## File-backed ledger audit

Use `FileLedgerSink` when you want a durable append-only record of timeline operations while keeping reverse replay behaviour unchanged.

```csharp
Ledger.Sink = new FileLedgerSink("Artifacts/Positronic/ledger.jsonl");
```

Each line is a JSON object containing:

- `Timestamp`
- `CommitId`
- `OperationName`
- `OperationType`

`FileLedgerSink.Clear()` clears the active in-memory replay ledger only. It does not truncate the audit file, so the on-disk trail remains append-only.