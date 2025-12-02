# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.7.5] - 2025-12-02

### Added
- Safety gateway: centralised `MutateTimeline(Action<IList<QuBit<T>>>)` on `PositronicVariable<T>`; all timeline mutations route through this gateway.
- Read-only public view: timeline is now exposed as `IReadOnlyList<QuBit<T>>`. External mutation of the live list is no longer possible.
- ConvergenceCoordinator: single-threaded coordinator that processes `IConvergenceWorkItem` via a bounded Channel queue; provides `FlushAsync()` for deterministic tests and queue/latency metrics.
- Exclusive engine token: coordinator holds an engine token recognised by `MutateTimeline` context checks.
- Transactional ledger: introduced `ILedgerSink` and buffered ledger entries (`BufferedLedgerEntry`) with per-commit identifiers to append exactly-once after successful commits.
- Immutable archival: `ImmutableTimelineSnapshot` and `Archivist.PublishSnapshot(snapshot)`; snapshots are cloned arrays detached from live lists.
- Hotspot telemetry: aggregation of abort rate, retries and lock-hold ticks per variable; hooks for pluggable `IHotspotMitigationStrategy` (e.g., sharding/single-writer promotion).
- Async-ready ambient context: `TransactionV2.Current` backed by `AsyncLocal<T>`; flat nested transaction semantics.
- Operator logging suppression: suppression is tied to the ambient transaction (AsyncLocal) instead of thread-static state.
- Model checking harness (short-run): linearizability/model-check based tests for small operation sets, with CI-friendly configuration.
- Paranoia mode: optional fail-fast diagnostics (lock ordering assertions, coordinator re-entry guards) and long-running stress harness configuration.

### Changed
- All convergence-related mutations now build/commit transactions through the coordinator; convergence/replay logic no longer mutates TVars directly.
- `QuantumLedgerOfRegret` operations are serialised and driven by transactions or coordinator hooks; direct stack manipulation is deprecated.
- Reverse reconstruction: replaced `dynamic` arithmetic with explicit numeric strategy/helpers to remove late binding.
- Concurrency docs: explicitly document single-threaded convergence and multi-threaded transactional updates.

### Fixed
- Data races around timeline mutation by guarding all mutation paths behind `MutateTimeline` with context assertions.
- Occasional ledger corruption under concurrent access by serialising and making append idempotent via commit-ids.
- Snapshot mutation hazards: archivist now receives immutable snapshots created post-commit, preventing shared mutable state.

### Performance
- Reduced GC pressure by avoiding live list exposure and by batching/deferring ledger appends to post-commit hooks.
- Coordinator-run convergence avoids unnecessary contention in user threads while keeping TVar lock windows short during commit.

### Diagnostics
- Debug guards: assert that convergence entry points are not invoked while `s_activeTransactions > 0`.
- Expanded STM telemetry report to include queue metrics and hotspot indicators when configured.

### Testing
- New tests for: mutation gateway guard, concurrent ledger appends, reverse replay numeric strategy behaviour, coordinator serialisation and `FlushAsync()`, immutable snapshot stability, async/nested transactions, linearizability smoke tests, and paranoia mode assertions.

### Notes
- Public API surface remains backward compatible for most usages; direct list mutation was never supported and is now technically prevented.
- Coordinator and transactional ledger are on by default and require no code changes for typical users.

## [1.5.0] - 2025-11-27

### Added
- STM Telemetry: commit/abort/retry counters, validation failures, total and max lock-hold ticks, writes applied, and contention hotspots by `TxId` (see `STMTelemetry`).
- Read-only fast path: transactions with no writes validate and commit without taking locks.
- Transaction APIs: `TransactionV2` and `TransactionScope` with bounded retries (exponential backoff + jitter), deterministic lock ordering and post-commit hooks.
- CI/Diagnostics: human-readable telemetry report via `STMTelemetry.GetReport()` and new tests covering read-only fast path, contention and report emission.

### Changed
- Integrated transactional apply for `PositronicVariable<T>` through `ITransactionalVariable` methods; version bumping and per-variable locking are handled within the transaction commit window.
- Hardened commit path to measure lock hold time and keep locks only across validate+apply.

### Fixed
- Reduced chances of deadlocks under contention by acquiring per-variable locks in id order.
- Ensured non-transactional writes bump version so transactions can detect conflicts reliably.

### Notes
- The STM layer is production-focused but the broader convergence engine remains not fully thread-safe across arbitrary concurrent use. Prefer performing multi-variable mutations under `TransactionScope` in concurrent scenarios.

## [1.4.0] - 2024-10-01

### Added
- Integration tests for enhanced reliability.
- Support for string manipulation and generic types.
- Dependency Injection support allowing scoped and testable runtime environments.
- Convergence engine builder with middleware chaining for flexible convergence logic.
- Epoch tagging for better timeline synchronisation in convergence loops.
- Running domain tracking to remember all values ever seen by variables.
- New operations: ReversibleModulusOp for modulus operations with undo support.
- Enhanced neural nodule functionality for building probabilistic circuits.
- Bitwise operators support (AND, OR, XOR, NOT, shifts) for integral types.
- Improved arithmetic operations with commutative caching and non observational modes.

### Changed
- Major refactor of PositronicVariable system for clarity and robustness.
- Updated design pattern to use GetOrCreate for variable instantiation.
- Enhanced timeline handling including better reset logic for outside writes.
- Refactored reverse replay and scalar close handling for improved convergence.
- Improved loop handling and domain reset logic.
- Updated tests and logging throughout the framework.
- Merged PositronicVariableRef and PositronicVariable into a single class.
- Enhanced README with better examples and documentation.
- Updated versions and dependencies across projects.

### Fixed
- Handling of closing replace operations and detection of "x = x + k" patterns.
- Timeline reset issues in ResetTimelineIfOutsideWrites.
- Convergence behaviour in various edge cases including scalar convergence.
- Console formatting and output issues in tests.
- QuantumLedgerOfRegret clearing before loop execution.
- Various bugs in reverse replay and forward handling.
- HTML entities in README.md.
- Error messages for non IComparable types.

### Removed
- Antival console formatting and related obsolete tests.

### Technical Improvements
- Better synchronisation and middleware support in convergence engine.
- Improved probabilistic and functional operator tests.
- Enhanced quantum gate and algorithm tests.
- Refactored comments and code structure for maintainability.
- Added support for complex numbers and advanced quantum operations.