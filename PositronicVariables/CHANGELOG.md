# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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