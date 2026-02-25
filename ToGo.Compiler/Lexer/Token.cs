namespace ToGo.Compiler.Lexer;

public readonly record struct Token(TokenKind Kind, int Position, string Text, object? Value = null);
