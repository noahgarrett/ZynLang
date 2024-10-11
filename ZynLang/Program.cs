using System.Diagnostics.Metrics;
using System.Text.Json;
using ZynLang.AST;
using ZynLang.Execution;
using ZynLang.Models;
namespace ZynLang;

internal class Program
{
    public static bool LexerDebug = true;
    public static bool ParserDebug = false;

    static void DebugLexer(Lexer lexer)
    {
        List<Token> tokens = [];
        Token lastToken = lexer.NextToken();
        tokens.Add(lastToken);
        while (lastToken.Type != TokenType.EOF)
        {
            lastToken = lexer.NextToken();
            tokens.Add(lastToken);
        }

        foreach (Token token in tokens)
        {
            Console.WriteLine(token.Print());
        }
    }

    static void Main(string[] args)
    {
        string source = "fn main() int { return 0; }";

        if (LexerDebug)
            DebugLexer(new Lexer(source));

        Lexer lexer = new(source);
        Parser parser = new(lexer);

        

        /*Token lastToken = lexer.NextToken();
        tokens.Add(lastToken);
        while (lastToken.Type != TokenType.EOF)
        {
            lastToken = lexer.NextToken();
            tokens.Add(lastToken);
        }

        foreach(Token token in tokens)
        {
            Console.WriteLine(token.Print());
        }*/

        
        ProgramNode programNode = parser.ParseProgram();
        if (parser.Errors.Count > 0)
        {
            foreach (var error in parser.Errors)
                Console.WriteLine($"Parser Error: {error}");
        }

        /*JsonSerializerOptions options = new()
        {
            WriteIndented = true,
        };

        foreach (var stmt in programNode.Statements)
        {
            Console.WriteLine(JsonSerializer.Serialize(stmt, options));
        }*/

        Compiler compiler = new();
        compiler.Run(programNode);
    }
}
