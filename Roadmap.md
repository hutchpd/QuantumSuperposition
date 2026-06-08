# ToGo Roadmap

## Vision
ToGo is the CLR-native language experiment for this repository: a language where beauty matters, quantum-style state is first-class, and PositronicVariables and QuantumSuperposition are not libraries you reach for, but part of the language model itself.

The current MVP in `ToGo.Compiler` proves the vertical slice: lexer, parser, binder, lowering, IL emit, and a minimal runtime bridge. The next phases should keep that simplicity while turning the language into something expressive, elegant, and unmistakably its own.

## Design principles
- Beauty first: syntax should be small, regular, and pleasant to read.
- Explicit collapse: quantum-like behaviour should have clear observation boundaries.
- CLR-native: generated code and runtime integration should feel like a first-class .NET feature, not a bolted-on DSL.
- First-class quantum variables: `QuantumSuperposition` and `PositronicVariables` should be language concepts, not foreign types.
- Predictable semantics: probabilistic behaviour should still be debuggable, testable, and reproducible.

## Roadmap
1. Stabilise the MVP compiler pipeline.
   - Keep the `.tg -> lexer -> parser -> AST -> binder -> lowering -> IL emit` flow simple and reliable.
   - Expand the current `int`-only surface carefully, without losing the small core.
   - Improve the language server entry point and diagnostics feedback loop.

2. Introduce language-level quantum variables.
   - Make `QuantumSuperposition` and `PositronicVariables` feel native in ToGo syntax.
   - Define how declarations, assignment, observation, and convergence map onto CLR/runtime behaviour.
   - Preserve the current runtime idea that observation is a meaningful event, not a silent conversion.

3. Prototype `SchrodingerObject`.
   - Treat this as the bridge between dynamic typing and controlled collapse.
   - Allow a value to hold multiple possible representations until a typed boundary or observer forces collapse.
   - Use this to explore superposition typing, mixed-domain values, and runtime type resolution.

4. Explore quantum currying.
   - Model partially applied functions as superposed execution paths.
   - Keep function composition elegant rather than magical.
   - Make the final observer or argument the point where a path is selected or resolved.

5. Define observer collapse semantics.
   - Decide which operations collapse, which only sample, and which preserve superposition.
   - Keep this visible in the language surface so developers can reason about it.
   - Support deterministic replay where possible.

6. Build tooling around the language shape.
   - Strengthen syntax highlighting, diagnostics, and language-server support.
   - Add sample programs that show the intended aesthetic and semantics.
   - Keep the compiler outputs easy to inspect and debug.

## Ideas raised in discussion
- A `SchrodingerObject` can act as a bridge to a quantum-typed dynamic language.
- Superposition typing could allow one variable to carry multiple possible types until evaluation.
- Typed collapse should happen at strict boundaries such as casts, observer calls, or typed assignment.
- Quantum currying can model partial application as entangled execution paths.
- The observer effect should be visible in the language, not hidden in runtime internals.
- The language should feel like a controlled rewrite of dynamic typing, not an accidental one.

## Open questions
- What is the smallest syntax that makes quantum state feel elegant rather than verbose?
- Should collapse be implicit at strict boundaries, or always require an explicit observer call?
- How much of `QuBit<T>` should surface directly versus through ToGo-specific wrappers?
- Should `PositronicVariables` appear as a distinct declaration form, or as a quantum-flavoured runtime type?
- What debugging model best shows the competing timelines before collapse?

## Near-term focus
- Keep the compiler MVP moving toward a usable language slice.
- Design the type and collapse model before expanding syntax too far.
- Treat `SchrodingerObject` as the first serious experiment in language-level superposition typing.
- Preserve the repository's aesthetic: small surfaces, strong ideas, and clean runtime boundaries.