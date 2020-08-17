using System;
using Frontend.Lexer;

namespace Frontend.AST
{
    public class ASTObject : IASTNode
    {
        private readonly NodePrototype prototype;

        private readonly IASTNode[] _values;
        
        public ASTObject(NodePrototype prototype, params IASTNode[] args)
        {
            this.prototype = prototype;
            _values = args;
        }

        public int Id()
            => prototype.Id();

        public IASTNode this[string name] 
            => _values[prototype.IdxOf(name)];

        public void Print(SymbolDictionary sd, string offset = "")
        {
            Console.WriteLine($"{offset}{prototype.Name()}:");
            foreach(var field in prototype.Names())
            {
                Console.WriteLine($"{offset}   {field}:");
                _values[prototype.IdxOf(field)].Print(sd, $"{offset}      ");
            }
        }
    }
}