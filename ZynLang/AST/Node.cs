﻿namespace ZynLang.AST;

public enum NodeType
{
    Program,

    // Statements
    ExpressionStatement, LetStatement, FunctionStatement, BlockStatement, ReturnStatement,
    AssignStatement, IfStatement, WhileStatement, BreakStatement, ContinueStatement, ForStatement,
    ImportStatement, ImportFromStatement,

    // Expressions
    InfixExpression, CallExpression, PrefixExpression, PostfixExpression, DotExpression, IndexExpression,

    // Literals
    IntegerLiteral, FloatLiteral, IdentifierLiteral, BooleanLiteral, StringLiteral, ArrayLiteral, HashLiteral,

    // Helpers
    FunctionParameter
}

public abstract class Node
{
    public abstract NodeType Type();

    public abstract Dictionary<string, object> Json();
}

public abstract class StatementNode : Node { }
public abstract class ExpressionNode : Node { }
