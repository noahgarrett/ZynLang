using LLVMSharp.Interop;
using System;
using System.Collections.Generic;
using ZynLang.AST.Literals;

namespace ZynLang.Execution;

public partial class Compiler
{
    #region Resolve Literals
    private (LLVMValueRef, LLVMTypeRef) ResolveIntegerValue(IntegerLiteralNode node)
    {
        return (LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, (ulong)node.Value), LLVMTypeRef.Int32);
    }

    private (LLVMValueRef, LLVMTypeRef) ResolveFloatValue(FloatLiteralNode node)
    {
        return (LLVMValueRef.CreateConstReal(LLVMTypeRef.Double, node.Value), LLVMTypeRef.Double);
    }

    private (LLVMValueRef, LLVMTypeRef) ResolveStringValue(StringLiteralNode node)
    {
        // Properly handle escape sequences
        string escapedValue = node.Value.Replace("\\n", "\n").Replace("\\t", "\t").Replace("\\\\", "\\");

        // Create a global constant for the string
        LLVMValueRef stringGlobal = _builder.BuildGlobalStringPtr(escapedValue);

        // Return the pointer to the string and its type (i8*)
        return (stringGlobal, LLVMTypeRef.CreatePointer(LLVMTypeRef.Int8, 0));
    }

    private (LLVMValueRef, LLVMTypeRef) ResolveArrayValue(ArrayLiteralNode node, string? valueType)
    {
        ArrayLiteralNode aNode = (ArrayLiteralNode)node;

        // TODO: Verify that valueType is not null
        var elementType = _typeMap[valueType];
        uint elementCount = (uint)aNode.Elements.Count;

        LLVMTypeRef arrayType = LLVMTypeRef.CreateArray(elementType, elementCount);
        LLVMValueRef arrayAlloc = _builder.BuildAlloca(arrayType);

        for (int i = 0; i < aNode.Elements.Count; i++)
        {
            (LLVMValueRef elementValue, LLVMTypeRef resolvedType) = ResolveValue(aNode.Elements[i]);

            // TODO: Verify the resolved type is the same as the arrays declared type

            LLVMValueRef elementPtr = _builder.BuildInBoundsGEP2(
                arrayType,
                arrayAlloc,
                [
                    LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, 0, false),       // Offset to the start of the array
                    LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, (ulong)i, false) // Current element index
                ]
            );

            _builder.BuildStore(elementValue, elementPtr);
        }

        return (arrayAlloc, arrayType);
    }

    private (LLVMValueRef, LLVMTypeRef) ResolveHashValue(HashLiteralNode node)
    {
        //LLVMValueRef mallocFn = _module.GetNamedFunction("malloc");
        LLVMTypeRef mallocType = LLVMTypeRef.CreateFunction(LLVMTypeRef.CreatePointer(LLVMTypeRef.Int8, 0), new LLVMTypeRef[] { LLVMTypeRef.Int64 }, false);
        LLVMValueRef mallocFn = _module.AddFunction(
            "malloc",
            mallocType
        );

        // Allocate memory for dict
        LLVMValueRef dictPtr = _builder.BuildCall2(
            mallocType,
            mallocFn,
            new LLVMValueRef[] { LLVMValueRef.CreateConstInt(LLVMTypeRef.Int64, 64, false) },
            "dict_ptr"
        );

        LLVMValueRef keysArray = _builder.BuildCall2(
            mallocType,
            mallocFn,
            new LLVMValueRef[] { LLVMValueRef.CreateConstInt(LLVMTypeRef.Int64, 128, false) },
            "keys_array"
        );

        LLVMValueRef valuesArray = _builder.BuildCall2(
            mallocType,
            mallocFn,
            new LLVMValueRef[] { LLVMValueRef.CreateConstInt(LLVMTypeRef.Int64, 64, false) },
            "values_array"
        );

        // Store the keys in the dict
        // TODO: allow multiple keys and values
        LLVMValueRef keysArrayPtr = _builder.BuildStructGEP2(
            mallocType, // MIGHT BE ISSUE
            dictPtr,
            0,
            "keys_array_ptr"
        );
        _builder.BuildStore(keysArray, keysArrayPtr);

        LLVMValueRef valuesArrayPtr = _builder.BuildStructGEP2(
            mallocType, // MIGHT BE ISSUE
            dictPtr,
            1,
            "values_array_ptr"
        );
        _builder.BuildStore(valuesArray, valuesArrayPtr);

        LLVMValueRef capacityPtr = _builder.BuildStructGEP2(
             mallocType, // MIGHT BE ISSUE
             dictPtr,
             2,
             "capacity_ptr"
        );
        _builder.BuildStore(LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, 16, false), capacityPtr);

        // Insert key-value pair
        foreach (var kvp in node.Pairs)
        {
            var (keyVal, keyType) = ResolveValue(kvp.Key);
            var (valVal, valType) = ResolveValue(kvp.Value);

            LLVMValueRef hashValue = _builder.BuildCall2(
                LLVMTypeRef.Int32,
                _module.GetNamedFunction("internal_hash"),
                new LLVMValueRef[] { keyVal },
                "hash_value"
            );

            LLVMValueRef capacity = _builder.BuildLoad2(LLVMTypeRef.Int32, capacityPtr, "capacity");
            LLVMValueRef index = _builder.BuildURem(hashValue, capacity, "index");

            // Store key in keys array
            LLVMValueRef keySlot = _builder.BuildGEP2(
                LLVMTypeRef.CreatePointer(LLVMTypeRef.Int8, 0),
                keysArray,
                new LLVMValueRef[] { index },
                "key_slot"
            );
            _builder.BuildStore(keyVal, keySlot);

            // Store value in values array
            LLVMValueRef valueSlot = _builder.BuildGEP2(
                LLVMTypeRef.Int32,
                valuesArray,
                new LLVMValueRef[] { index },
                "value_slot"
            );
            _builder.BuildStore(valVal, valueSlot);

            break; // TODO: make sure this works with multiple key value pairs
        }

        return (dictPtr, LLVMTypeRef.CreatePointer(LLVMTypeRef.Int8, 0));
    }

    private (LLVMValueRef, LLVMTypeRef) ResolveBooleanValue(BooleanLiteralNode node)
    {
        int boolConv = node.Value ? 1 : 0;
        return (LLVMValueRef.CreateConstInt(LLVMTypeRef.Int1, (ulong)boolConv), LLVMTypeRef.Int1);
    }

    private (LLVMValueRef, LLVMTypeRef) ResolveIdentifierValue(IdentifierLiteralNode node)
    {
        var (ptr, type) = ((LLVMValueRef, LLVMTypeRef))_env.Lookup(node.Value);

        if (type.Kind == LLVMTypeKind.LLVMArrayTypeKind)
        {
            return (ptr, type);
        }

        return (_builder.BuildLoad2(GetReturnType(type), ptr), GetReturnType(type));
    }
    #endregion
}