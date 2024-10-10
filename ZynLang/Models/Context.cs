
using LLVMSharp.Interop;

namespace ZynLang.Models;

public class Context(Dictionary<string, (LLVMValueRef, LLVMTypeRef)>? records = null, Context? parent = null, string name = "global")
{
    public Dictionary<string, (LLVMValueRef, LLVMTypeRef)> Records { get; set; } = records ?? [];
    public Context? Parent { get; set; } = parent;
    public string Name { get; set; } = name;

    public LLVMValueRef Define(string name, LLVMValueRef value, LLVMTypeRef type)
    {
        Records[name] = (value, type);
        return value;
    }

    public (LLVMValueRef, LLVMTypeRef)? Lookup(string name)
    {
        return Resolve(name);
    }

    public (LLVMValueRef, LLVMTypeRef)? Resolve(string name)
    {
        if (Records.TryGetValue(name, out var value))
            return Records[name];
        else if (Parent != null)
            return Parent.Resolve(name);
        else
            return null;
    }
}
