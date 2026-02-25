# ToGo.Compiler (MVP)

This project implements the smallest end-to-end vertical slice for the ToGo language:

```
.tg source -> lexer -> parser -> AST -> binder -> lowering -> IL emit (Mono.Cecil) -> .NET console app
```

## Supported syntax (MVP)

- `antival<int> name;`
- `time { ... }`
- `name := expression;`
- `print(expression);`
- integer literals, identifiers, and `+`

Only `int` is supported.

## Semantics (MVP)

- `antival<int>` lowers to `PositronicVariable<int>`.
- A `time { ... }` block is *converged* using the `PositronicVariables` runtime.
- `print(...)` inside `time` is **deferred** until after convergence.
- Printing does **not** collapse: it prints `QuBit<T>.ToString()` (no `Observe()`).

## Runtime shim

To keep emitted IL simple, the generated program calls into:

- `ToGo.Compiler.Runtime.ToGoRuntime` (runtime initialisation + printing helpers)
- `ToGo.Compiler.Runtime.TimeRunner` (runs a convergence action and flushes the coordinator)

## Emitted program shape

The code generator emits:

- a `Program` type
- one static field per antival
- one static method per `time` block (`__togo_time_0`, ...)
- `Main`:
  1. initialises the positronic runtime for `int`
  2. creates antivals
  3. runs each time block through `TimeRunner.Run(...)`
  4. executes deferred prints

## Example

See `examples/example.tg`.

Build and run (from an output folder containing the generated `example.dll`):

```
ToGo.LanguageServer ..\ToGo.Compiler\examples\example.tg out\example.dll

dotnet out\example.dll
```

Expected output is the uncollapsed `QuBit<int>` representation of `x` after convergence (e.g. `any(4)`).
