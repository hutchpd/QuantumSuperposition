namespace ToGo.Compiler.Lexer;

public enum TokenKind
{
    BadToken,
    EndOfFile,

    // Trivia/Identifiers/Literals
    Identifier,
    IntegerLiteral,

    // Keywords
    AntivalKeyword,
    TimeKeyword,
    PrintKeyword,
    IntKeyword,

    // Punctuation
    LessThan,
    GreaterThan,
    OpenBrace,
    CloseBrace,
    OpenParen,
    CloseParen,
    Semicolon,

    // Operators
    Plus,
    ColonEquals,
}
