using System.Collections.Generic;
using ToGo.Compiler.Lexer;

namespace ToGo.Compiler.AST;

public abstract record SyntaxNode;

public sealed record ProgramNode(IReadOnlyList<StatementNode> Statements) : SyntaxNode;

public abstract record StatementNode : SyntaxNode;

public sealed record AntivalDeclarationNode(string Name) : StatementNode;

public sealed record TimeBlockNode(IReadOnlyList<StatementNode> Statements) : StatementNode;

public sealed record PrintStatementNode(ExpressionNode Expression) : StatementNode;

public sealed record AssignmentStatementNode(string Name, ExpressionNode Expression) : StatementNode;

public abstract record ExpressionNode : SyntaxNode;

public sealed record IntegerLiteralNode(int Value) : ExpressionNode;

public sealed record IdentifierExpressionNode(string Name) : ExpressionNode;

public sealed record BinaryExpressionNode(ExpressionNode Left, TokenKind OperatorKind, ExpressionNode Right) : ExpressionNode;
