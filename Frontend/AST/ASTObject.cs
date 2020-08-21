using System;
using System.Collections.Generic;
using Frontend.Lexer;

namespace Frontend.AST
{
    public class ASTObject : IASTNode
    {
        public readonly NodePrototype Prototype;

        public readonly IASTNode[] Values;
        
        public ASTObject(NodePrototype prototype, params IASTNode[] args)
        {
            this.Prototype = prototype;
            Values = args;
        }

        public int Id()
            => Prototype.Id();

        public IASTNode this[string name] 
            => Values[Prototype.IdxOf(name)];

        public IEnumerable<IASTNode> Enumerate()
            => Values;

        public void Print(SymbolDictionary sd, string offset = "")
        {
            Console.WriteLine($"{offset}{Prototype.Name()}:");
            foreach(var field in Prototype.Names())
            {
                Console.WriteLine($"{offset}   {field}:");
                Values[Prototype.IdxOf(field)].Print(sd, $"{offset}      ");
            }
        }
    }
}