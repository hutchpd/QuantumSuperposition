using System.Collections.Generic;
using ToGo.Compiler.Binder;

namespace ToGo.Compiler.Lowering;

public sealed record LoweredProgram(
    IReadOnlyList<VariableSymbol> Antivals,
    IReadOnlyList<LoweredTimeBlock> TimeBlocks);

public sealed record LoweredTimeBlock(
    IReadOnlyList<BoundAssignmentStatement> Assignments,
    IReadOnlyList<BoundExpression> DeferredPrints);
