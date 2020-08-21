using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Frontend.Lexer;

namespace Frontend.AST
{
    public interface IASTNode
    {
        int Id();

        IASTNode this[string name] { get; }

        IEnumerable<IASTNode> Enumerate();
            
        public void Print(SymbolDictionary sd, string offset = "");
    }
}