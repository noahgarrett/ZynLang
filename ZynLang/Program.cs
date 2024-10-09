using System.Text.Json;
using ZynLang.AST;
using ZynLang.Execution;
using static System.Net.Mime.MediaTypeNames;
using ZynLang.Models;
namespace ZynLang;

internal class Program
{
    static void Main(string[] args)
    {
        Lexer lexer = new("let a = 5;");
        //Parser parser = new(lexer);

        List<Token> tokens = [];

        Token lastToken = lexer.NextToken();
        tokens.Add(lastToken);
        while (lastToken.Type != TokenType.EOF)
        {
            lastToken = lexer.NextToken();
            tokens.Add(lastToken);
        }

        foreach(Token token in tokens)
        {
            Console.WriteLine(token.Print());
        }

        /*
        ProgramNode programNode = parser.ParseProgram();
        if (parser.Errors.Count > 0)
        {
            foreach (var error in parser.Errors)
                Console.WriteLine($"Parser Error: {error}");
        }

        Console.WriteLine(JsonSerializer.Serialize(programNode));*/
    }
}
