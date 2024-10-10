using ZynLang.AST;
using ZynLang.Execution;

namespace ZynLangTests.Execution.Tests;

[TestClass]
public class ParserTests
{
    [TestMethod]
    [DataRow("let a: int = 4;", NodeType.LetStatement)]
    public void NodeTypeTests(string test, NodeType expectedNT)
    {
        Lexer lexer = new(test);
        Parser parser = new(lexer);

        ProgramNode programNode = parser.ParseProgram();
        if (parser.Errors.Count > 0)
        {
            string errorOutput = string.Empty;

            foreach (var error in parser.Errors)
                errorOutput += $"\n- Parser Error: {error}";

            Assert.Fail(errorOutput);
        }

        if (programNode.Statements.Count == 0)
            Assert.Fail("The program statements length was zero.");

        Assert.AreEqual(programNode.Statements[0].Type(), expectedNT);
    }
}
