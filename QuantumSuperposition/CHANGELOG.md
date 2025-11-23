# Changelog

All notable changes to this project will be documented in this file.

The format follows [Keep a Changelog](https://keepachangelog.com/en/1.0.0/) and the project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.7.5] - 2025-11-23
### Added
- Fix: `QuantumSystem.SetFromTensorProduct` now constructs system-managed `QuBit<T>` instances using the `QuBit(QuantumSystem, int[])` constructor for local qubits passed into the method. These qubits are registered with the system and preserve qubit indices so collapse propagation, partial observation and entanglement bookkeeping behave correctly.
- Extensibility: `SetFromTensorProduct<T>` now accepts an optional `Func<T,int>` basis mapper to support non-int/bool basis types (e.g., enums or custom types). Defaults provided for `int`, `bool`, and any enum via `Convert.ToInt32`.
- API: Back-compat overload `SetFromTensorProduct<T>(bool propagateCollapse, params QuBit<T>[] qubits)` retained; it uses the default mapper.
- Tests updated: new tests cover enum basis mapping and custom mapper usage; entanglement/collapse tests exercise corrected tensor-product wiring and registration behaviour.

### Fixed
- Bug: Previously `SetFromTensorProduct` created temporary local `QuBit<T>` instances that were not registered with the containing `QuantumSystem`, which caused missing `System` references, lost qubit indices, and incomplete collapse/entanglement propagation. This has been corrected.

### Notes
- This release targets reliability of entanglement and measurement propagation and adds flexible basis mapping. No breaking public API changes.

## [1.7.4] - 2025-11-19
### Added
- Initial changelog (this file) establishing version history tracking.
- `QuantumRegister` abstraction for coherent grouped qubit index handling (partial collapse, integer decoding, amplitude initialisation).
- `PhysicsQubit` specialised computational basis qubit with Bloch sphere constructors and `Zero` / `One` shortcuts.
- Canonical state factories: `EPRPair`, `WState`, `GHZState` via `QuantumRegister` helpers.
- Gate * register operator sugar: apply single / multi qubit gates directly with `gate * register` syntax.
- Extended gate catalogue: identity tensor factories, multi length Hadamard, Pauli Y/Z, SWAP, √SWAP, Toffoli (CCNOT), Fredkin (CSWAP), generic `Controlled(inner)`, monolithic Quantum Fourier Transform unitary factory.
- Quantum algorithms API: high level `QuantumAlgorithms.QuantumFourierTransform` and `QuantumAlgorithms.GroverSearch` with internal gate scheduling.
- Weighted superposition support with complex amplitudes across `QuBit<T>` and `Eigenstates<T>` plus deterministic seeded sampling and mock collapse injection.
- Functional and LINQ style non collapse operators: `Select`, `Where`, `SelectMany`, `p_op`, `p_func` preserving superpositions.
- Non observational arithmetic flag (`QuantumConfig.EnableNonObservationalArithmetic`) enabling scalar and pairwise arithmetic without forced collapse; includes commutative optimisation cache.
- Tolerant equality: `QuantumRegister.AlmostEquals`, `Eigenstates<T>.AlmostEquals` probability mass based comparisons.
- Entanglement graph diagnostics and versioning utilities; tagging, partial collapse staging, locking / freezing and multi party collapse agreement.
- Performance: Multi-qubit gate application now uses structural grouping (pattern arrays with sentinel -1) instead of string.Join which previously allocated per state; avoids transient string objects and reduces GC pressure in large Hilbert spaces.

### Changed
- README completely restructured for clarity: capability sections, documentation map, British English spelling, concise feature matrix.
- Documentation split into focused markdown files (FunctionalOps, Entanglement, ComplexSupport, QuantumAlgorithms, PhysicsQubit, QuantumRegister, GateRegisterSugar, GateCatalogue, CanonicalStates, Equality, UsageExamples).
- Gate scheduling visualisation wording updated to British English ("visualise").
- Consistent terminology: "normalise", "initialisation", "behaviour", "optimisation" across code comments and docs.

### Fixed
- Numerous wording inconsistencies in documentation (American to British English spellings).
- Minor formatting issues in previous README (duplicate headings, em dash usage removed).

### Removed
- Legacy monolithic README narrative sections superseded by modular docs.

### Performance
- Commutative optimisation avoids duplicate evaluation of pure operations (e.g. caching results for `a + b` vs `b + a`).
- Select / Where chains retain lazy non collapse semantics reducing unnecessary sampling.
- Multi-qubit gate grouping no longer performs string concatenations; structural pattern hashing accelerates Grover / QFT circuits.

### Documentation
- Added usage examples for prime detection, factors, minimum value extraction and multi qubit gate application.
- Expanded algorithm breakdown (QFT gate sequence, Grover diffusion operator internals).

### Notes
- First published changelog; earlier versions are retroactively undocumented here.
