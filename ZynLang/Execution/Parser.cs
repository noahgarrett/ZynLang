using ZynLang.AST;
using ZynLang.AST.Expressions;
using ZynLang.AST.Helpers;
using ZynLang.AST.Literals;
using ZynLang.AST.Statements;
using ZynLang.Models;

namespace ZynLang.Execution;

public enum PrecedenceType { LOWEST, EQUALS, LESSGREATER, SUM, PRODUCT, EXPONENT, PREFIX, CALL, INDEX }

public static class ParserHelper
{
    public static readonly Dictionary<TokenType, int> Precedences = new()
    {
        {TokenType.ILLEGAL, (int)PrecedenceType.LOWEST },

        {TokenType.PLUS, (int)PrecedenceType.SUM },
        {TokenType.MINUS, (int)PrecedenceType.SUM },

        {TokenType.SLASH, (int)PrecedenceType.PRODUCT },
        {TokenType.ASTERISK, (int)PrecedenceType.PRODUCT },
        {TokenType.MODULUS, (int)PrecedenceType.PRODUCT },

        {TokenType.POW, (int)PrecedenceType.EXPONENT },

        {TokenType.EQ_EQ, (int)PrecedenceType.EQUALS },
        {TokenType.NOT_EQ, (int)PrecedenceType.EQUALS },

        {TokenType.LT, (int)PrecedenceType.LESSGREATER },
        {TokenType.GT, (int)PrecedenceType.LESSGREATER },
        {TokenType.LT_EQ, (int)PrecedenceType.LESSGREATER },
        {TokenType.GT_EQ, (int)PrecedenceType.LESSGREATER },

        {TokenType.LPAREN, (int)PrecedenceType.CALL },

        {TokenType.PLUS_PLUS, (int)PrecedenceType.INDEX },
        {TokenType.MINUS_MINUS, (int)PrecedenceType.INDEX },
    };

    public static readonly List<TokenType> AssignmentOperators = [TokenType.EQ, TokenType.PLUS_EQ, TokenType.MINUS_EQ, TokenType.MUL_EQ, TokenType.DIV_EQ];
}

public delegate Node? PrefixFunction();
public delegate Node? InfixFunction(ExpressionNode expr);

public class Parser
{
    public Lexer ZynLexer { get; set; }

    public List<string> Errors { get; set; } = [];

    public Token? CurrentToken { get; set; } = null;
    public Token? PeekToken { get; set; } = null;

    public Parser(Lexer lexer)
    {
        ZynLexer = lexer;

        // Populate the currentToken and peekToken values from lexer
        NextToken();
        NextToken();
    }

    public ProgramNode ParseProgram()
    {
        ProgramNode program = new();

        while (CurrentToken?.Type != TokenType.EOF)
        {
            StatementNode? stmt = ParseStatement();
            if (stmt != null)
                program.Statements.Add(stmt);

            NextToken();
        }

        return program;
    }

    #region Statement Methods
    private StatementNode? ParseStatement()
    {
        if (CurrentToken?.Type == TokenType.IDENT && PeekTokenIsAssignment())
            return ParseAssignStatement();

        return (CurrentToken?.Type) switch
        {
            TokenType.LET => ParseLetStatement(),
            TokenType.FN => ParseFunctionStatement(),
            TokenType.RETURN => ParseReturnStatement(),
            TokenType.WHILE => ParseWhileStatement(),
            TokenType.BREAK => ParseBreakStatement(),
            TokenType.CONTINUE => ParseContinueStatement(),
            TokenType.FOR => ParseForStatement(),
            TokenType.IMPORT => ParseImportStatement(),
            _ => ParseExpressionStatement(),
        };
    }

    private AssignStatementNode ParseAssignStatement()
    {
        IdentifierLiteralNode ident = new(CurrentToken?.Literal);
        NextToken();

        string op = CurrentToken?.Literal ?? string.Empty;
        NextToken();

        ExpressionNode rightValue = ParseExpression(PrecedenceType.LOWEST);

        NextToken();

        return new AssignStatementNode(ident, op, rightValue);
    }

    private IfStatementNode? ParseIfStatement()
    {
        ExpressionNode condition;
        BlockStatementNode consequence;
        BlockStatementNode? alternative = null;

        NextToken();

        condition = ParseExpression(PrecedenceType.LOWEST);

        if (!ExpectPeek(TokenType.LBRACE))
            return null;

        consequence = ParseBlockStatement();

        if (PeekTokenIs(TokenType.ELSE))
        {
            NextToken();

            if (!ExpectPeek(TokenType.LBRACE))
                return null;

            alternative = ParseBlockStatement();
        }

        return new IfStatementNode(condition, consequence, alternative);
    }

    private LetStatementNode? ParseLetStatement()
    {
        if (!ExpectPeek(TokenType.IDENT))
            return null;

        IdentifierLiteralNode varName = new((string?)CurrentToken?.Literal ?? "");

        if (!ExpectPeek(TokenType.COLON))
            return null;

        if (!ExpectPeek(TokenType.TYPE))
            return null;

        string valueType = CurrentToken?.Literal ?? string.Empty;

        if (!ExpectPeek(TokenType.EQ))
            return null;

        NextToken();

        ExpressionNode valueNode = ParseExpression(PrecedenceType.LOWEST);

        while (!CurrentTokenIs(TokenType.SEMICOLON) && !CurrentTokenIs(TokenType.EOF))
            NextToken();

        return new LetStatementNode(varName, valueNode, valueType);
    }

    private FunctionStatementNode? ParseFunctionStatement()
    {
        if (!ExpectPeek(TokenType.IDENT))
            return null;

        IdentifierLiteralNode funName = new(CurrentToken?.Literal);

        if (!ExpectPeek(TokenType.LPAREN))
            return null;

        List<FunctionParameterNode>? parameters = ParseFunctionParameters();
        if (parameters == null)
        {
            Errors.Add($"Error parsing parameters for: {funName}");
            return null;
        }

        NextToken();

        string returnType = CurrentToken?.Literal ?? string.Empty;

        if (!ExpectPeek(TokenType.LBRACE))
            return null;

        BlockStatementNode body = ParseBlockStatement();

        return new FunctionStatementNode(parameters, body, funName, returnType);
    }

    private BlockStatementNode ParseBlockStatement()
    {
        List<StatementNode> statements = [];
        NextToken();

        while (!CurrentTokenIs(TokenType.RBRACE) && !CurrentTokenIs(TokenType.EOF))
        {
            StatementNode? stmt = ParseStatement();
            if (stmt != null)
                statements.Add(stmt);

            NextToken();
        }

        return new BlockStatementNode(statements);
    }

    private ReturnStatementNode? ParseReturnStatement()
    {
        NextToken();

        ExpressionNode returnValue = ParseExpression(PrecedenceType.LOWEST);

        if (!ExpectPeek(TokenType.SEMICOLON))
            return null;

        return new ReturnStatementNode(returnValue);
    }

    private WhileStatementNode? ParseWhileStatement()
    {
        ExpressionNode condition;
        BlockStatementNode body;

        NextToken();

        condition = ParseExpression(PrecedenceType.LOWEST);

        if (!ExpectPeek(TokenType.LBRACE))
            return null;

        body = ParseBlockStatement();

        return new WhileStatementNode(condition, body);
    }

    private BreakStatementNode ParseBreakStatement()
    {
        NextToken();
        return new BreakStatementNode();
    }

    private ContinueStatementNode ParseContinueStatement()
    {
        NextToken();
        return new ContinueStatementNode();
    }

    /// <summary>
    /// for (let i: int = 0; i < 10; i = i + 1) { }
    /// </summary>
    /// <returns></returns>
    private ForStatementNode? ParseForStatement()
    {
        if (!ExpectPeek(TokenType.LPAREN))
            return null;

        if (!ExpectPeek(TokenType.LET))
            return null;

        LetStatementNode? varDeclaration = ParseLetStatement();
        if (varDeclaration == null)
            return null;

        NextToken();

        ExpressionNode condition = ParseExpression(PrecedenceType.LOWEST);

        if (!ExpectPeek(TokenType.SEMICOLON))
            return null;

        NextToken();

        //AssignStatementNode action = ParseAssignStatement();
        ExpressionNode action = ParseExpression(PrecedenceType.LOWEST);

        if (!ExpectPeek(TokenType.RPAREN))
            return null;

        if (!ExpectPeek(TokenType.LBRACE))
            return null;

        BlockStatementNode body = ParseBlockStatement();

        return new ForStatementNode(varDeclaration, condition, action, body);
    }

    private ImportStatementNode? ParseImportStatement()
    {
        if (!ExpectPeek(TokenType.STRING))
            return null;
        
        ImportStatementNode? stmt = new(CurrentToken?.Literal ?? "");

        if (!ExpectPeek(TokenType.SEMICOLON))
            return null;

        return stmt;
    }

    private ExpressionStatementNode ParseExpressionStatement()
    {
        ExpressionNode expr = ParseExpression(PrecedenceType.LOWEST);

        if (PeekTokenIs(TokenType.SEMICOLON))
            NextToken();

        return new ExpressionStatementNode(expr);
    }
    #endregion

    #region Statement Helpers
    private List<FunctionParameterNode>? ParseFunctionParameters()
    {
        List<FunctionParameterNode> parameters = [];

        if (PeekTokenIs(TokenType.RPAREN))
        {
            NextToken();
            return parameters;
        }

        NextToken();

        string firstParamName = CurrentToken?.Literal ?? "";

        if (!ExpectPeek(TokenType.COLON))
        {
            Errors.Add($"Expected colon to declare value type for param: {firstParamName}");
            return null;
        }

        NextToken();

        string firstParamType = CurrentToken?.Literal ?? "";

        parameters.Add(new(firstParamName, firstParamType));

        while (PeekTokenIs(TokenType.COMMA))
        {
            NextToken();
            NextToken();

            string pName = CurrentToken?.Literal ?? "";

            if (!ExpectPeek(TokenType.COLON))
            {
                Errors.Add($"Expected colon to declare value type for param: {firstParamName}");
                return null;
            }

            NextToken();

            string pType = CurrentToken?.Literal ?? "";

            parameters.Add(new(pName, pType));
        }

        if (!ExpectPeek(TokenType.RPAREN))
        {
            Errors.Add($"Expected RPAREN to close function parameter list");
            return null;
        }

        return parameters;
    }
    #endregion
    
    #region Expression Functions
    private ExpressionNode? ParseExpression(PrecedenceType precedence)
    {
        PrefixFunction? prefixFn = GetPrefixFunction(CurrentToken?.Type ?? TokenType.ILLEGAL);
        if (prefixFn == null)
        {
            NoPrefixParseFnError(CurrentToken?.Type ?? TokenType.ILLEGAL);
            return null;
        }

        Node? leftExpr = prefixFn();
        while (!PeekTokenIs(TokenType.SEMICOLON) && (int)precedence < (int)PeekPrecedence())
        {
            InfixFunction? infixFn = GetInfixFunction(PeekToken?.Type ?? TokenType.ILLEGAL);
            if (infixFn == null)
                return (ExpressionNode?)leftExpr;

            NextToken();

            leftExpr = infixFn((ExpressionNode)leftExpr);
        }

        return (ExpressionNode?)leftExpr;
    }

    private ExpressionNode ParseInfixExpression(ExpressionNode leftNode)
    {
        string op = CurrentToken?.Literal ?? string.Empty;

        PrecedenceType precedence = CurrentPrecedence();

        NextToken();

        ExpressionNode rightNode = ParseExpression(precedence);

        return new InfixExpressionNode(leftNode, op, rightNode);
    }

    private PostfixExpressionNode ParsePostfixExpression(ExpressionNode leftNode)
    {
        return new PostfixExpressionNode(CurrentToken?.Literal ?? string.Empty, leftNode);
    }

    private ExpressionNode? ParseGroupedExpression()
    {
        NextToken();

        ExpressionNode expr = ParseExpression(PrecedenceType.LOWEST);

        if (!ExpectPeek(TokenType.RPAREN))
            return null;

        return expr;
    }

    private CallExpressionNode ParseCallExpression(ExpressionNode function)
    {
        return new CallExpressionNode((IdentifierLiteralNode)function, ParseExpressionList(TokenType.RPAREN));
    } 

    private List<ExpressionNode> ParseExpressionList(TokenType end)
    {
        List<ExpressionNode> eList = [];


        if (PeekTokenIs(end))
        {
            NextToken();
            return eList;
        }

        NextToken();

        eList.Add(ParseExpression(PrecedenceType.LOWEST));

        while (PeekTokenIs(TokenType.COMMA))
        {
            NextToken();
            NextToken();

            eList.Add(ParseExpression(PrecedenceType.LOWEST));
        }

        if (!ExpectPeek(end))
        {
            Errors.Add($"Did not find {end} token at the end of the expression list...");
            return [];
        }

        return eList;
    }
    #endregion

    #region Prefix Methods
    private IdentifierLiteralNode ParseIdentifier()
    {
        return new IdentifierLiteralNode(CurrentToken?.Literal ?? string.Empty);
    }

    private IntegerLiteralNode ParseIntLiteral()
    {
        return new IntegerLiteralNode((int?)CurrentToken?.Literal ?? 0);
    }

    private FloatLiteralNode ParseFloatLiteral()
    {
        return new FloatLiteralNode((float?)CurrentToken?.Literal ?? 0f);
    }

    private BooleanLiteralNode ParseBooleanLiteral()
    {
        return new BooleanLiteralNode(CurrentTokenIs(TokenType.TRUE));
    }

    private StringLiteralNode ParseStringLiteral()
    {
        return new StringLiteralNode(CurrentToken?.Literal ?? string.Empty);
    }

    private PrefixExpressionNode ParsePrefixExpression()
    {
        string op = CurrentToken?.Literal ?? string.Empty;
        NextToken();

        return new PrefixExpressionNode(op, ParseExpression(PrecedenceType.PREFIX));
    }
    #endregion

    #region Helper Functions
    private PrefixFunction? GetPrefixFunction(TokenType tt)
    {
        return tt switch
        {
            TokenType.IDENT => ParseIdentifier,
            TokenType.INT => ParseIntLiteral,
            TokenType.FLOAT => ParseFloatLiteral,
            TokenType.LPAREN => ParseGroupedExpression,
            TokenType.IF => ParseIfStatement,
            TokenType.TRUE => ParseBooleanLiteral,
            TokenType.FALSE => ParseBooleanLiteral,
            TokenType.STRING => ParseStringLiteral,
            TokenType.MINUS => ParsePrefixExpression,
            TokenType.BANG => ParsePrefixExpression,
            _ => null,
        };
    }

    private InfixFunction? GetInfixFunction(TokenType tt)
    {
        return tt switch
        {
            TokenType.PLUS => ParseInfixExpression,
            TokenType.MINUS => ParseInfixExpression,
            TokenType.SLASH => ParseInfixExpression,
            TokenType.ASTERISK => ParseInfixExpression,
            TokenType.POW => ParseInfixExpression,
            TokenType.MODULUS => ParseInfixExpression,
            TokenType.EQ_EQ => ParseInfixExpression,
            TokenType.NOT_EQ => ParseInfixExpression,
            TokenType.LT => ParseInfixExpression,
            TokenType.GT => ParseInfixExpression,
            TokenType.LT_EQ => ParseInfixExpression,
            TokenType.GT_EQ => ParseInfixExpression,
            TokenType.LPAREN => ParseCallExpression,
            TokenType.PLUS_PLUS => ParsePostfixExpression,
            TokenType.MINUS_MINUS => ParsePostfixExpression,
            _ => null,
        };
    }

    private void NextToken()
    {
        CurrentToken = PeekToken;
        PeekToken = ZynLexer.NextToken();
    }

    private bool CurrentTokenIs(TokenType type) => CurrentToken?.Type == type;
    private bool PeekTokenIs(TokenType type) => PeekToken?.Type == type;
    private bool PeekTokenIsAssignment() => ParserHelper.AssignmentOperators.Contains(PeekToken?.Type ?? TokenType.EOF);

    private bool ExpectPeek(TokenType type)
    {
        if (PeekTokenIs(type))
        {
            NextToken();
            return true;
        }
        else
        {
            PeekError(type);
            return false;
        }
    }

    private PrecedenceType CurrentPrecedence()
    {
        ParserHelper.Precedences.TryGetValue(CurrentToken?.Type ?? TokenType.EOF, out int prec);
        return (PrecedenceType)prec;
    }
    private PrecedenceType PeekPrecedence()
    {
        ParserHelper.Precedences.TryGetValue(PeekToken?.Type ?? TokenType.EOF, out int prec);
        return (PrecedenceType)prec;
    }

    private void PeekError(TokenType type) => Errors.Add($"Expected next token to be {nameof(type)}, got {nameof(PeekToken.Type)}");
    private void NoPrefixParseFnError(TokenType type) => Errors.Add($"No Prefix Parse Function for {type} found.");
    #endregion
}
