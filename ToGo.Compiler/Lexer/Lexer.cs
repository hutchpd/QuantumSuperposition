using System;
using System.Collections.Generic;
using System.Globalization;
using ToGo.Compiler.Diagnostics;

namespace ToGo.Compiler.Lexer;

public sealed class Lexer
{
    private readonly string _text;
    private readonly DiagnosticBag _diagnostics;
    private int _pos;

    public Lexer(string text, DiagnosticBag diagnostics)
    {
        _text = text ?? throw new ArgumentNullException(nameof(text));
        _diagnostics = diagnostics ?? throw new ArgumentNullException(nameof(diagnostics));
    }

    public IEnumerable<Token> Lex()
    {
        while (true)
        {
            var token = NextToken();
            yield return token;
            if (token.Kind == TokenKind.EndOfFile)
            {
                yield break;
            }
        }
    }

    private Token NextToken()
    {
        SkipWhitespace();

        var start = _pos;
        if (_pos >= _text.Length)
        {
            return new Token(TokenKind.EndOfFile, _pos, string.Empty);
        }

        char c = _text[_pos];

        if (IsIdentifierStart(c))
        {
            _pos++;
            while (_pos < _text.Length && IsIdentifierPart(_text[_pos]))
            {
                _pos++;
            }

            string text = _text.Substring(start, _pos - start);
            var kind = text switch
            {
                "antival" => TokenKind.AntivalKeyword,
                "time" => TokenKind.TimeKeyword,
                "print" => TokenKind.PrintKeyword,
                "int" => TokenKind.IntKeyword,
                _ => TokenKind.Identifier
            };

            return new Token(kind, start, text);
        }

        if (char.IsDigit(c))
        {
            _pos++;
            while (_pos < _text.Length && char.IsDigit(_text[_pos]))
            {
                _pos++;
            }

            string text = _text.Substring(start, _pos - start);
            if (!int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out int value))
            {
                _diagnostics.Report(start, $"Invalid integer literal: '{text}'.");
                value = 0;
            }
            return new Token(TokenKind.IntegerLiteral, start, text, value);
        }

        _pos++;
        switch (c)
        {
            case '<': return new Token(TokenKind.LessThan, start, "<");
            case '>': return new Token(TokenKind.GreaterThan, start, ">");
            case '{': return new Token(TokenKind.OpenBrace, start, "{");
            case '}': return new Token(TokenKind.CloseBrace, start, "}");
            case '(': return new Token(TokenKind.OpenParen, start, "(");
            case ')': return new Token(TokenKind.CloseParen, start, ")");
            case ';': return new Token(TokenKind.Semicolon, start, ";");
            case '+': return new Token(TokenKind.Plus, start, "+");
            case ':':
                if (_pos < _text.Length && _text[_pos] == '=')
                {
                    _pos++;
                    return new Token(TokenKind.ColonEquals, start, ":=");
                }
                break;
        }

        _diagnostics.Report(start, $"Unexpected character '{c}'.");
        return new Token(TokenKind.BadToken, start, c.ToString());
    }

    private void SkipWhitespace()
    {
        while (_pos < _text.Length)
        {
            char c = _text[_pos];
            if (c == ' ' || c == '\t' || c == '\r' || c == '\n')
            {
                _pos++;
                continue;
            }
            break;
        }
    }

    private static bool IsIdentifierStart(char c)
        => char.IsLetter(c) || c == '_';

    private static bool IsIdentifierPart(char c)
        => char.IsLetterOrDigit(c) || c == '_';
}
