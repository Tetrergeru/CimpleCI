using System;
using System.Runtime.InteropServices;
using Frontend.Lexer;

namespace Frontend.AST
{
    public interface IASTNode
    {
        int Id();

        IASTNode this[string name] { get; }

        public void Print(SymbolDictionary sd, string offset = "");
    }
}