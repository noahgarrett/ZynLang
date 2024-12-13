using ZynLang.Models;

namespace ZynLang.Execution;

public class Lexer
{
    public string Source { get; set; } = string.Empty;

    public int Position { get; set; } = -1;
    public int ReadPosition { get; set; } = 0;
    public int LineNo { get; set; } = 1;

    public char CurrentChar { get; set; }

    public Lexer(string source)
    {
        Source = source;
        readChar();
    }

    /// <summary>
    /// Advances the lexer and returns the next token
    /// </summary>
    /// <returns></returns>
    public Token NextToken()
    {
        Token tok;

        skipWhitespace();
        skipComment();

        switch (CurrentChar)
        {
            case '+':
                if (peekChar() == '=') // Handle +=
                {
                    char ch = CurrentChar;
                    readChar();
                    tok = newToken(TokenType.PLUS_EQ, $"{ch}{CurrentChar}");
                }
                else if (peekChar() == '+') // Handle ++
                {
                    char ch = CurrentChar;
                    readChar();
                    tok = newToken(TokenType.PLUS_PLUS, $"{ch}{CurrentChar}");
                }
                else // Handle +
                {
                    tok = newToken(TokenType.PLUS, $"{CurrentChar}");
                }
                break;
            case '-':
                if (peekChar() == '=') // Handle -=
                {
                    char ch = CurrentChar;
                    readChar();
                    tok = newToken(TokenType.MINUS_EQ, $"{ch}{CurrentChar}");
                }
                else if (peekChar() == '-') // Handle --
                {
                    char ch = CurrentChar;
                    readChar();
                    tok = newToken(TokenType.MINUS_MINUS, $"{ch}{CurrentChar}");
                }
                else // Handle -
                    tok = newToken(TokenType.MINUS, $"{CurrentChar}");
                break;
            case '*':
                if (peekChar() == '=') // Handle *=
                {
                    char ch = CurrentChar;
                    readChar();
                    tok = newToken(TokenType.MUL_EQ, $"{ch}{CurrentChar}");
                }
                else
                    tok = newToken(TokenType.ASTERISK, $"{CurrentChar}");
                break;
            case '/':
                if (peekChar() == '=') // Handle /=
                {
                    char ch = CurrentChar;
                    readChar();
                    tok = newToken(TokenType.DIV_EQ, $"{ch}{CurrentChar}");
                }
                else
                    tok = newToken(TokenType.SLASH, $"{CurrentChar}");
                break;
            case '^':
                tok = newToken(TokenType.POW, $"{CurrentChar}");
                break;
            case '%':
                tok = newToken(TokenType.MODULUS, $"{CurrentChar}");
                break;
            case '<':
                if (peekChar() == '=') // Handle <=
                {
                    char ch = CurrentChar;
                    readChar();
                    tok = newToken(TokenType.LT_EQ, $"{ch}{CurrentChar}");
                }
                else
                    tok = newToken(TokenType.LT, $"{CurrentChar}");
                break;
            case '>':
                if (peekChar() == '=') // Handle >=
                {
                    char ch = CurrentChar;
                    readChar();
                    tok = newToken(TokenType.GT_EQ, $"{ch}{CurrentChar}");
                }
                else
                    tok = newToken(TokenType.GT, $"{CurrentChar}");
                break;
            case '=':
                if (peekChar() == '=') // Handle ==
                {
                    char ch = CurrentChar;
                    readChar();
                    tok = newToken(TokenType.EQ_EQ, $"{ch}{CurrentChar}");
                }
                else
                    tok = newToken(TokenType.EQ, $"{$"{CurrentChar}"}");
                break;
            case '!':
                if (peekChar() == '=') // Handle !=
                {
                    char ch = CurrentChar;
                    readChar();
                    tok = newToken(TokenType.NOT_EQ, $"{ch}{CurrentChar}");
                }
                else
                    tok = newToken(TokenType.BANG, $"{CurrentChar}");
                break;
            case ':':
                tok = newToken(TokenType.COLON, $"{CurrentChar}");
                break;
            case ';':
                tok = newToken(TokenType.SEMICOLON, $"{CurrentChar}");
                break;
            case ',':
                tok = newToken(TokenType.COMMA, $"{CurrentChar}");
                break;
            case '(':
                tok = newToken(TokenType.LPAREN, $"{CurrentChar}");
                break;
            case ')':
                tok = newToken(TokenType.RPAREN, $"{CurrentChar}");
                break;
            case '{':
                tok = newToken(TokenType.LBRACE, $"{CurrentChar}");
                break;
            case '}':
                tok = newToken(TokenType.RBRACE, $"{CurrentChar}");
                break;
            case '"':
                tok = newToken(TokenType.STRING, readString());
                break;
            case '[':
                tok = newToken(TokenType.LBRACKET, $"{CurrentChar}");
                break;
            case ']':
                tok = newToken(TokenType.RBRACKET, $"{CurrentChar}");
                break;
            case '\0':
                tok = newToken(TokenType.EOF, $"{CurrentChar}");
                break;
            default:
                if (isLetter(CurrentChar))
                {
                    string literal = readIdentifier();
                    TokenType tt = TokenHelper.LookupIdent(literal);
                    tok = newToken(tt, $"{literal}");
                    return tok;
                }
                else if (isDigit(CurrentChar))
                {
                    tok = readNumber();
                    return tok;
                }
                else
                    tok = newToken(TokenType.ILLEGAL, $"{CurrentChar}");
                break;
        }

        readChar();
        return tok;
    }

    private void skipComment()
    {
        if (CurrentChar == '/' && peekChar() == '/')
        {
            readChar();
            readChar();

            while (CurrentChar != '\n')
                readChar();

            // Skip the \n
            readChar();
        }
    }

    /// <summary>
    /// Reads the next character from the source code and advances the lexer position
    /// </summary>
    private void readChar()
    {
        if (ReadPosition >= Source.Length)
            CurrentChar = '\0';
        else
            CurrentChar = Source[ReadPosition];

        Position = ReadPosition;
        ReadPosition++;
    }

    /// <summary>
    /// Peeks to the upcoming character without advancing the lexer position
    /// </summary>
    /// <returns></returns>
    private char? peekChar()
    {
        if (ReadPosition >= Source.Length)
            return null;

        return Source[ReadPosition];
    }

    /// <summary>
    /// Skips ignored characters and whitespace
    /// </summary>
    private void skipWhitespace()
    {
        while (" \t\n\r".Contains(CurrentChar))
        {
            if (CurrentChar == '\n')
                LineNo++;

            readChar();
        }
    }

    /// <summary>
    /// Creates a new token class
    /// </summary>
    /// <param name="tt"></param>
    /// <param name="literal"></param>
    /// <returns></returns>
    private Token newToken(TokenType tt, dynamic literal)
    {
        return new Token(tt, literal, LineNo, Position);
    }

    /// <summary>
    /// Verifies if the character should be treated as a letter
    /// </summary>
    /// <param name="ch"></param>
    /// <returns></returns>
    private bool isLetter(char ch)
    {
        return 'a' <= ch && ch <= 'z' || 'A' <= ch && ch <= 'Z' || ch == '_';
    }

    private bool isDigit(char ch)
    {
        return '0' <= ch && ch <= '9';
    }

    /// <summary>
    /// Reads an identifier name
    /// </summary>
    /// <returns></returns>
    private string readIdentifier()
    {
        int position = Position;
        while (CurrentChar != '\0' && (isLetter(CurrentChar) || isDigit(CurrentChar)))
            readChar();

        return Source[position..Position];
    }

    /// <summary>
    /// Reads a number from the input file and returns its token
    /// </summary>
    /// <returns></returns>
    private Token readNumber()
    {
        int startPos = Position;
        int dotCount = 0;

        string output = string.Empty;
        while (isDigit(CurrentChar) || CurrentChar == '.')
        {
            if (CurrentChar == '.')
                dotCount++;

            if (dotCount > 1)
            {
                // TODO: fix this you dumb ass :(
                Console.WriteLine($"Too many decimals in number on line {LineNo}, position {Position}");
                return newToken(TokenType.ILLEGAL, Source[startPos..Position]);
            }

            output += Source[Position];
            readChar();

            if (CurrentChar == '\0')
                break;
        }

        if (dotCount == 0)
            return newToken(TokenType.INT, int.Parse(output));
        else
            return newToken(TokenType.FLOAT, float.Parse(output));
    }

    private string readString()
    {
        int startPos = Position + 1;

        while (true)
        {
            readChar();
            if (CurrentChar == '"' || CurrentChar == '\0')
                break;
        }

        return Source[startPos..Position];
    }
}
