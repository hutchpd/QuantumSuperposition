# Contributing

Thanks for considering a contribution to QuantumSuperposition and PositronicVariables.

This repository contains two related .NET libraries:
- QuantumSuperposition: strongly typed superpositions and quantum-style simulation tools
- PositronicVariables: timeline convergence and reversible temporal logic built on top of QuBit<T>

## Ground Rules

- Keep changes focused and scoped to a clear goal.
- Preserve public API behavior unless the change is explicitly a breaking change.
- Add or update tests for every behavior change.
- Update documentation when adding, removing, or changing features.
- Keep commits readable and messages descriptive.

## Prerequisites

- .NET SDK 8.0 or newer
- Git
- Optional: Visual Studio 2022 or VS Code with C# tooling

## Local Setup

1. Clone the repository.
2. From the repository root, restore dependencies:

```bash
dotnet restore QuantumSuperposition.sln
```

3. Build everything:

```bash
dotnet build QuantumSuperposition.sln -c Release
```

## Running Tests

Run the full test suite:

```bash
dotnet test QuantumSuperposition.sln -c Release
```

If you are working in one area, run the relevant test project first (for faster iteration), then run the full suite before opening a PR.

## Coding Guidelines

- Prefer clear, explicit code over clever shortcuts.
- Keep naming consistent with existing code in each project.
- Avoid unrelated refactors in feature or bug-fix pull requests.
- Keep thread-safety and determinism in mind, especially in transaction/convergence paths.
- For user-facing changes, update README/docs/changelog where appropriate.

## Pull Request Checklist

Before opening a pull request, please verify:

- The solution builds cleanly.
- Relevant tests pass locally.
- New behavior is covered by tests.
- Documentation is updated if needed.
- Changelog entries are added where appropriate.

In your PR description, include:

- What changed
- Why it changed
- Any behavior or API impact
- How it was tested

## Issues and Discussions

Please open issues for:
- Bugs and regressions
- Test failures or non-deterministic behavior
- Documentation gaps
- Feature proposals

A minimal repro is very helpful when reporting bugs.

## Security

If you discover a security issue, please do not post exploit details publicly in a new issue. Share enough detail for maintainers to reproduce safely and coordinate a fix.

## License

By contributing, you agree that your contributions are released under the repository license (Unlicense).

Thanks for helping improve the multiverse.
