using LLVMSharp.Interop;
using System.Runtime.InteropServices;
using ZynLang.AST;
using ZynLang.AST.Expressions;
using ZynLang.AST.Helpers;
using ZynLang.AST.Literals;
using ZynLang.AST.Statements;
using ZynLang.Models;

namespace ZynLang.Execution;

public partial class Compiler
{
    private LLVMModuleRef _module;
    private LLVMBuilderRef _builder;
    private LLVMPassManagerRef _passManager;
    private LLVMExecutionEngineRef _engine;

    private LLVMValueRef _mainFunction;
    private LLVMValueRef _currentFunction;

    private Context _env;

    private Dictionary<string, LLVMTypeRef> _typeMap;

    private uint counter = 0;

    private List<LLVMBasicBlockRef> Breakpoints = [];
    private List<LLVMBasicBlockRef> Continues = [];

    private readonly Dictionary<LLVMBasicBlockRef, bool> _blockTerminators = [];

    private readonly string StandardLibraryPath = Path.Combine(AppContext.BaseDirectory, "BuiltIns", "std");

    public List<string> Errors = [];

    public Compiler()
    {
        _typeMap = new()
        {
            {"int", LLVMTypeRef.Int32 },
            {"float", LLVMTypeRef.Double },
            {"bool", LLVMTypeRef.Int1 },
            {"void", LLVMTypeRef.Void },
            {"str", LLVMTypeRef.CreatePointer(LLVMTypeRef.Int8, 0) },
            {"dict", LLVMTypeRef.CreateStruct([
                LLVMTypeRef.CreatePointer(LLVMTypeRef.CreatePointer(LLVMTypeRef.Int8, 0), 0),
                LLVMTypeRef.CreatePointer(LLVMTypeRef.Int32, 0),
                LLVMTypeRef.Int32
            ], false) },    // TODO: Allow values other than integers

            {"arr_int", LLVMTypeRef.Int32 },
            {"arr_float", LLVMTypeRef.Double },
            {"arr_bool", LLVMTypeRef.Int1 },
            {"arr_str", LLVMTypeRef.CreateArray(LLVMTypeRef.CreatePointer(LLVMTypeRef.Int8, 0), 0) },
        };

        _env = new();

        LLVM.LinkInMCJIT();
        LLVM.InitializeX86TargetMC();
        LLVM.InitializeX86Target();
        LLVM.InitializeX86TargetInfo();
        LLVM.InitializeX86AsmParser();
        LLVM.InitializeX86AsmPrinter();

        InitializeModule();

        SetupBuiltinFunctions();    // Functions pre-built for users

        //SetupInternalFunctions();   // Functions used internally for compiler
    }

    public void Run(ProgramNode node)
    {
        VisitProgram(node);
    }

    public void Execute()
    {
        _module.PrintToString();
        _module.Verify(LLVMVerifierFailureAction.LLVMPrintMessageAction);

        var res = _engine.RunFunction(_mainFunction, Array.Empty<LLVMGenericValueRef>());

        ulong resultAsInt = 2;

        unsafe
        {
            resultAsInt = LLVM.GenericValueToInt(res, 1);
        }
        

        Console.WriteLine("\n> {0}", resultAsInt);

        _builder.Dispose();
        _module.Dispose();
        //_engine.Dispose();
    }

    // Function to extract the return type from a function type
    LLVMTypeRef GetReturnType(LLVMTypeRef type)
    {
        if (type.Kind == LLVMTypeKind.LLVMFunctionTypeKind) // Check if the type is a function type
        {
            return type.ReturnType; // Extract the return type
        }
        else if (type.Kind == LLVMTypeKind.LLVMArrayTypeKind)
        {
            return type.ElementType;
        }

        return type; // Otherwise, it's already the return type
    }

    private uint IncrementCounter()
    {
        counter++;
        return counter;
    }

    // 1 + 1;
    private void Compile(Node node)
    {
        switch (node.Type())
        {
            case NodeType.Program:
                VisitProgram((ProgramNode)node);
                break;

            // Statements
            case NodeType.ExpressionStatement:
                VisitExpressionStatement((ExpressionStatementNode)node); 
                break;
            case NodeType.LetStatement:
                VisitLetStatement((LetStatementNode)node); 
                break;
            case NodeType.FunctionStatement:
                VisitFunctionStatement((FunctionStatementNode)node);
                break;
            case NodeType.ReturnStatement:
                VisitReturnStatement((ReturnStatementNode)node);
                break;
            case NodeType.BlockStatement:
                VisitBlockStatement((BlockStatementNode)node);
                break;
            case NodeType.IfStatement:
                VisitIfStatement((IfStatementNode)node);
                break;
            case NodeType.AssignStatement:
                VisitAssignStatement((AssignStatementNode)node);
                break;
            case NodeType.WhileStatement:
                VisitWhileStatement((WhileStatementNode)node);
                break;
            case NodeType.ForStatement:
                VisitForStatement((ForStatementNode)node);
                break;
            case NodeType.BreakStatement:
                VisitBreakStatement((BreakStatementNode)node);
                break;
            case NodeType.ContinueStatement:
                VisitContinueStatement((ContinueStatementNode)node);
                break;
            case NodeType.ImportStatement:
                VisitImportStatement((ImportStatementNode)node);
                break;
            case NodeType.ImportFromStatement:
                VisitImportFromStatement((ImportFromStatementNode)node);
                break;

            // Expressions
            case NodeType.InfixExpression:
                VisitInfixExpression((InfixExpressionNode)node);
                break;
            case NodeType.CallExpression:
                VisitCallExpression((CallExpressionNode)node);
                break;
            case NodeType.PostfixExpression:
                VisitPostfixExpression((PostfixExpressionNode)node);
                break;
            case NodeType.IndexExpression:
                VisitIndexExpression((IndexExpressionNode)node);
                break;
        }
    }

    private void InitializeModule()
    {
        _module = LLVMModuleRef.CreateWithName("ZynLang Module");
        _builder = _module.Context.CreateBuilder();

        // Initialize all optimization passes
        /*_passManager = _module.CreateFunctionPassManager();
        _passManager.AddBasicAliasAnalysisPass();
        _passManager.AddPromoteMemoryToRegisterPass();
        _passManager.AddInstructionCombiningPass();
        _passManager.AddReassociatePass();
        _passManager.AddGVNPass();
        _passManager.AddCFGSimplificationPass();
        _passManager.InitializeFunctionPassManager();*/

        _engine = _module.CreateMCJITCompiler();
    }

    private static unsafe LLVMModuleRef LoadModule(string filePath)
    {
        // These will hold the results from the LLVM functions.
        LLVMOpaqueMemoryBuffer* memBuffer = null;
        sbyte* errorMessagePtr = null;

        // Convert the file path to an ANSI string (sbyte*) for LLVM.
        IntPtr filePathIntPtr = Marshal.StringToHGlobalAnsi(filePath);
        sbyte* filePathPtr = (sbyte*)filePathIntPtr.ToPointer();

        // Create a memory buffer from the file.
        int result = LLVM.CreateMemoryBufferWithContentsOfFile(filePathPtr, &memBuffer, &errorMessagePtr);
        // Free the HGlobal allocated for the file path.
        Marshal.FreeHGlobal(filePathIntPtr);

        if (result != 0)
        {
            string errorMessage = Marshal.PtrToStringAnsi((IntPtr)errorMessagePtr);
            throw new Exception($"Error reading file {filePath}: {errorMessage}");
        }

        // Now, parse the IR from the memory buffer.
        LLVMOpaqueModule* module;
        result = LLVM.ParseIRInContext(LLVM.GetGlobalContext(), memBuffer, &module, &errorMessagePtr);
        if (result != 0)
        {
            string errorMessage = Marshal.PtrToStringAnsi((IntPtr)errorMessagePtr);
            throw new Exception($"Error parsing IR in file {filePath}: {errorMessage}");
        }

        // Return the loaded module.
        return module;
    }

    private static unsafe bool LinkModules(LLVMModuleRef mainModule, LLVMModuleRef otherModule)
    {
        int linkResult = LLVM.LinkModules2(mainModule, otherModule);
        return linkResult == 0;
    }

    #region Helper Visit Methods
    private (LLVMValueRef, LLVMTypeRef) ResolveValue(ExpressionNode node, string? valueType = null)
    {
        switch (node.Type())
        {
            case NodeType.IntegerLiteral:
                return ResolveIntegerValue((IntegerLiteralNode)node);
            case NodeType.FloatLiteral:
                return ResolveFloatValue((FloatLiteralNode)node);
            case NodeType.StringLiteral:
                return ResolveStringValue((StringLiteralNode)node);
            case NodeType.ArrayLiteral:
                return ResolveArrayValue((ArrayLiteralNode)node, valueType);
            case NodeType.HashLiteral:
                return ResolveHashValue((HashLiteralNode)node);
            case NodeType.BooleanLiteral:
                return ResolveBooleanValue((BooleanLiteralNode)node);
            case NodeType.IdentifierLiteral:
                return ResolveIdentifierValue((IdentifierLiteralNode)node);

            // Expression Values
            case NodeType.InfixExpression:
                return VisitInfixExpression((InfixExpressionNode)node);
            case NodeType.CallExpression:
                return VisitCallExpression((CallExpressionNode)node);
            case NodeType.PrefixExpression:
                return VisitPrefixExpression((PrefixExpressionNode)node);
            case NodeType.IndexExpression:
                return VisitIndexExpression((IndexExpressionNode)node);

            default:
                Console.WriteLine("RESOLVE VALUE WENT TO DEFAULT WTF DUDE");
                return (LLVMValueRef.CreateConstReal(LLVMTypeRef.Int32, 69), LLVMTypeRef.Int32);
        }
    }
    #endregion

    #region Compiler Setup
    private void SetupBuiltinFunctions()
    {
        // Declare printf
        LLVMTypeRef printfType = LLVMTypeRef.CreateFunction(
            LLVMTypeRef.Int32,
            new[] { LLVMTypeRef.CreatePointer(LLVMTypeRef.Int8, 0) },
            IsVarArg: true
        );

        LLVMValueRef printfFunction = _module.AddFunction("printf", printfType);
        _env.Define("print", printfFunction, printfType);
    }

    private void SetupInternalFunctions()
    {
        // Hash function
        SetupInternalHashFunction();

        // Malloc 64-bit
        //LLVMTypeRef mallocType = LLVMTypeRef.CreateFunction(
        //    LLVMTypeRef.CreatePointer(LLVMTypeRef.Int8, 0),
        //    [LLVMTypeRef.Int64],
        //    false
        //);
        //_module.AddFunction("malloc", mallocType);
    }

    private void SetupInternalHashFunction()
    {
        LLVMTypeRef hashFuncType = LLVMTypeRef.CreateFunction(
            LLVMTypeRef.Int32,
            [LLVMTypeRef.CreatePointer(LLVMTypeRef.Int32, 0)],
            false
        );

        LLVMValueRef hashFunc = _module.AddFunction("internal_hash", hashFuncType);

        LLVMBasicBlockRef entryBlock = hashFunc.AppendBasicBlock("entry");
        _builder.PositionAtEnd(entryBlock);

        // Initialize hash
        LLVMValueRef hash = _builder.BuildAlloca(LLVMTypeRef.Int32, "hash");
        _builder.BuildStore(LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, 5381, false), hash);
        LLVMValueRef ptr = hashFunc.GetParam(0); // Input string pointer

        // Jump to the loop block
        LLVMBasicBlockRef loopBlock = hashFunc.AppendBasicBlock("hash_loop");
        _builder.BuildBr(loopBlock);

        // Loop block
        _builder.PositionAtEnd(loopBlock);
        LLVMValueRef currentChar = _builder.BuildLoad2(
            LLVMTypeRef.Int8,
            _builder.BuildGEP2(LLVMTypeRef.Int8, ptr, new LLVMValueRef[] {
        LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, 0, false)
            }, "char_ptr"),
            "current_char"
        );

        // Check if the character is null (end of string)
        LLVMValueRef isNull = _builder.BuildICmp(
            LLVMIntPredicate.LLVMIntEQ,
            currentChar,
            LLVMValueRef.CreateConstInt(LLVMTypeRef.Int8, 0, false),
            "is_null"
        );

        LLVMBasicBlockRef endBlock = hashFunc.AppendBasicBlock("hash_end");
        LLVMBasicBlockRef bodyBlock = hashFunc.AppendBasicBlock("hash_body");

        // Conditional branch based on null check
        _builder.BuildCondBr(isNull, endBlock, bodyBlock);

        // Body block (process character and update hash)
        _builder.PositionAtEnd(bodyBlock);
        LLVMValueRef hashValue = _builder.BuildLoad2(LLVMTypeRef.Int32, hash, "current_hash");
        LLVMValueRef hashUpdated = _builder.BuildAdd(
            _builder.BuildMul(hashValue, LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, 33, false), "hash_mul"),
            _builder.BuildZExt(currentChar, LLVMTypeRef.Int32, "hash_char_ext"),
            "new_hash"
        );
        _builder.BuildStore(hashUpdated, hash);

        // Advance pointer to the next character
        ptr = _builder.BuildGEP2(
            LLVMTypeRef.Int8,
            ptr,
            new LLVMValueRef[] { LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, 1, false) },
            "next_char"
        );

        // Loop back
        _builder.BuildBr(loopBlock);

        // End block (return final hash)
        _builder.PositionAtEnd(endBlock);
        LLVMValueRef finalHash = _builder.BuildLoad2(LLVMTypeRef.Int32, hash, "final_hash");
        _builder.BuildRet(finalHash);
    }
    #endregion

    #region Private Helpers
    /// <summary>
    /// Checks to see if a LLVM type is a i8*
    /// </summary>
    /// <param name="rType"></param>
    /// <param name="lType"></param>
    private bool IsStringType(LLVMTypeRef type) => type.Kind == LLVMTypeKind.LLVMIntegerTypeKind && type.IntWidth == 8;
    #endregion
}