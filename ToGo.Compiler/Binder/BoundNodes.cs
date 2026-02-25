using System.Collections.Generic;
using ToGo.Compiler.Lexer;

namespace ToGo.Compiler.Binder;

public sealed record BoundProgram(IReadOnlyList<BoundStatement> Statements);

public abstract record BoundStatement;

public sealed record BoundAntivalDeclaration(VariableSymbol Variable) : BoundStatement;

public sealed record BoundTimeBlock(IReadOnlyList<BoundStatement> Statements) : BoundStatement;

public sealed record BoundPrintStatement(BoundExpression Expression) : BoundStatement;

public sealed record BoundAssignmentStatement(VariableSymbol Target, BoundExpression Expression) : BoundStatement;

public abstract record BoundExpression;

public sealed record BoundIntegerLiteral(int Value) : BoundExpression;

public sealed record BoundVariableExpression(VariableSymbol Variable) : BoundExpression;

public sealed record BoundBinaryExpression(BoundExpression Left, TokenKind OperatorKind, BoundExpression Right) : BoundExpression;
