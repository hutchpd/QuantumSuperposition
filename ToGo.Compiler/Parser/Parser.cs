using System;
using System.Collections.Generic;
using ToGo.Compiler.AST;
using ToGo.Compiler.Diagnostics;
using ToGo.Compiler.Lexer;

namespace ToGo.Compiler.Parser;

public sealed class Parser
{
    private readonly Token[] _tokens;
    private readonly DiagnosticBag _diagnostics;
    private int _pos;

    public Parser(string text, DiagnosticBag diagnostics)
    {
        _diagnostics = diagnostics ?? throw new ArgumentNullException(nameof(diagnostics));

        var lexer = new ToGo.Compiler.Lexer.Lexer(text ?? string.Empty, diagnostics);
        var tokens = new List<Token>();
        foreach (var t in lexer.Lex())
        {
            if (t.Kind != TokenKind.BadToken)
            {
                tokens.Add(t);
            }
        }
        _tokens = tokens.ToArray();
    }

    public ProgramNode ParseProgram()
    {
        var statements = new List<StatementNode>();
        while (Current.Kind != TokenKind.EndOfFile)
        {
            var stmt = ParseStatement(inTimeBlock: false);
            statements.Add(stmt);
        }
        return new ProgramNode(statements);
    }

    private StatementNode ParseStatement(bool inTimeBlock)
    {
        if (Current.Kind == TokenKind.AntivalKeyword)
        {
            if (inTimeBlock)
            {
                _diagnostics.Report(Current.Position, "antival declarations are only allowed at top-level in the MVP.");
            }
            return ParseAntivalDeclaration();
        }

        if (Current.Kind == TokenKind.TimeKeyword)
        {
            return ParseTimeBlock();
        }

        if (Current.Kind == TokenKind.PrintKeyword)
        {
            if (!inTimeBlock)
            {
                _diagnostics.Report(Current.Position, "print statements are only allowed inside a time block in the MVP.");
            }
            return ParsePrintStatement();
        }

        if (Current.Kind == TokenKind.Identifier)
        {
            if (!inTimeBlock)
            {
                _diagnostics.Report(Current.Position, "assignments are only allowed inside a time block in the MVP.");
            }
            return ParseAssignmentStatement();
        }

        _diagnostics.Report(Current.Position, $"Unexpected token '{Current.Kind}'.");
        var bad = NextToken();
        return new PrintStatementNode(new IdentifierExpressionNode(bad.Text));
    }

    private AntivalDeclarationNode ParseAntivalDeclaration()
    {
        Expect(TokenKind.AntivalKeyword);
        Expect(TokenKind.LessThan);
        Expect(TokenKind.IntKeyword);
        Expect(TokenKind.GreaterThan);
        var id = Expect(TokenKind.Identifier);
        Expect(TokenKind.Semicolon);
        return new AntivalDeclarationNode(id.Text);
    }

    private TimeBlockNode ParseTimeBlock()
    {
        Expect(TokenKind.TimeKeyword);
        Expect(TokenKind.OpenBrace);

        var statements = new List<StatementNode>();
        while (Current.Kind != TokenKind.CloseBrace && Current.Kind != TokenKind.EndOfFile)
        {
            var stmt = ParseStatement(inTimeBlock: true);
            statements.Add(stmt);
        }

        Expect(TokenKind.CloseBrace);
        return new TimeBlockNode(statements);
    }

    private PrintStatementNode ParsePrintStatement()
    {
        Expect(TokenKind.PrintKeyword);
        Expect(TokenKind.OpenParen);
        var expr = ParseExpression();
        Expect(TokenKind.CloseParen);
        Expect(TokenKind.Semicolon);
        return new PrintStatementNode(expr);
    }

    private AssignmentStatementNode ParseAssignmentStatement()
    {
        var id = Expect(TokenKind.Identifier);
        Expect(TokenKind.ColonEquals);
        var expr = ParseExpression();
        Expect(TokenKind.Semicolon);
        return new AssignmentStatementNode(id.Text, expr);
    }

    private ExpressionNode ParseExpression()
    {
        // additive only
        var left = ParsePrimaryExpression();
        while (Current.Kind == TokenKind.Plus)
        {
            var op = NextToken();
            var right = ParsePrimaryExpression();
            left = new BinaryExpressionNode(left, op.Kind, right);
        }
        return left;
    }

    private ExpressionNode ParsePrimaryExpression()
    {
        if (Current.Kind == TokenKind.IntegerLiteral)
        {
            var lit = NextToken();
            return new IntegerLiteralNode((int)(lit.Value ?? 0));
        }

        if (Current.Kind == TokenKind.Identifier)
        {
            var id = NextToken();
            return new IdentifierExpressionNode(id.Text);
        }

        if (Current.Kind == TokenKind.OpenParen)
        {
            NextToken();
            var expr = ParseExpression();
            Expect(TokenKind.CloseParen);
            return expr;
        }

        _diagnostics.Report(Current.Position, $"Expected an expression but found '{Current.Kind}'.");
        var bad = NextToken();
        return new IdentifierExpressionNode(bad.Text);
    }

    private Token Current => Peek(0);

    private Token Peek(int offset)
    {
        var index = _pos + offset;
        if (index >= _tokens.Length)
        {
            return _tokens[^1];
        }
        return _tokens[index];
    }

    private Token NextToken()
    {
        var t = Current;
        _pos++;
        return t;
    }

    private Token Expect(TokenKind kind)
    {
        if (Current.Kind == kind)
        {
            return NextToken();
        }

        _diagnostics.Report(Current.Position, $"Expected '{kind}' but found '{Current.Kind}'.");
        return new Token(kind, Current.Position, string.Empty);
    }
}
