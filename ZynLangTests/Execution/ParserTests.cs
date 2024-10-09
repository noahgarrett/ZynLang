using ZynLang.AST;
using ZynLang.Execution;

namespace ZynLangTests.Execution.Tests;

[TestClass]
public class ParserTests
{
    [TestMethod]
    [DataRow("let a = 4;", NodeType.LetStatement)]
    public void NodeTypeTests(string test, NodeType expectedNT)
    {
        Lexer lexer = new(test);
        Parser parser = new(lexer);

        ProgramNode programNode = parser.ParseProgram();
        if (parser.Errors.Count > 0)
        {
            foreach (var error in parser.Errors)
                Console.WriteLine($"Parser Error: {error}");

            Assert.Fail();
        }

        if (programNode.Statements.Count == 0)
            Assert.Fail("The program statements length was zero.");

        Assert.AreEqual(programNode.Statements[0].Type(), expectedNT);
    }
}
