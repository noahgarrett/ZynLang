namespace ZynLang.Models;

public enum TokenType
{
    EOF, ILLEGAL,

    // Data Types
    IDENT, INT, FLOAT, STRING,

    // Arithmetic Types
    PLUS, MINUS, ASTERISK, SLASH, POW, MODULUS,

    // Prefix Symbols
    BANG,

    // Postfix Symbols
    PLUS_PLUS, MINUS_MINUS,

    // Assignment Symbols
    EQ, PLUS_EQ, MINUS_EQ, MUL_EQ, DIV_EQ,

    // Comparison Symbols
    LT, LT_EQ, GT, GT_EQ, EQ_EQ, NOT_EQ,

    // Symbols
    COLON, COMMA, SEMICOLON, LPAREN, RPAREN, LBRACE, RBRACE, LBRACKET, RBRACKET,

    // Keywords
    LET, FN, RETURN, IF, ELSE, TRUE, FALSE, WHILE, BREAK, CONTINUE, FOR, 
    IMPORT, FROM, EXPORT,

    // Typing
    TYPE
}

public class Token(TokenType type, dynamic? literal, int lineNo, int position) : IEquatable<Token>
{
    public TokenType Type { get; set; } = type;
    public dynamic? Literal { get; set; } = literal;
    public int LineNo { get; set; } = lineNo;
    public int Position { get; set; } = position;

    public string Print()
    {
        return $"Token[{Type} : {Literal} : Line {LineNo} : Position {Position}]";
    }

    public bool Equals(Token? other)
    {
        if (other == null)
            return false;

        return Type == other.Type && Literal == other.Literal;
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as Token);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Type, Literal);
    }
}

public static class TokenHelper
{
    public static Dictionary<string, TokenType> Keywords = new()
    {
        { "let", TokenType.LET },
        { "fn", TokenType.FN },
        { "return", TokenType.RETURN },
        { "if", TokenType.IF },
        { "else", TokenType.ELSE },
        { "true", TokenType.TRUE },
        { "false", TokenType.FALSE },
        { "while", TokenType.WHILE },
        { "break", TokenType.BREAK },
        { "continue", TokenType.CONTINUE },
        { "for", TokenType.FOR },
        { "import", TokenType.IMPORT },
        { "from", TokenType.FROM },
        { "export", TokenType.EXPORT },
    };

    public static Dictionary<string, TokenType> AltKeywords = new()
    {
        { "lit", TokenType.LET },
        { "rn", TokenType.SEMICOLON },
        { "be", TokenType.EQ },
        { "bruh", TokenType.FN },
        { "pause", TokenType.RETURN },
        { "sus", TokenType.IF },
        { "imposter", TokenType.ELSE },
        { "nocap", TokenType.TRUE },
        { "cap", TokenType.FALSE },
        { "weee", TokenType.WHILE },
        { "yeet", TokenType.BREAK },
        { "anothaone", TokenType.CONTINUE },
        { "dab", TokenType.FOR },
        { "gib", TokenType.IMPORT },
    };

    public static List<string> TypeKeywords = [
        "int", "float", "bool", "str", "void", "dict",

        "arr_int", "arr_float", "arr_bool", "arr_str",
    ];

    public static TokenType LookupIdent(string ident)
    {
        bool normalKeywordExists = Keywords.TryGetValue(ident, out TokenType nType);
        if (normalKeywordExists)
            return nType;

        bool altKeywordExists = AltKeywords.TryGetValue(ident, out TokenType aType);
        if (altKeywordExists)
            return aType;

        if (TypeKeywords.Contains(ident))
            return TokenType.TYPE;

        return TokenType.IDENT;
    }
}
