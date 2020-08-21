using System;
using System.Collections.Generic;
using System.Linq;
using Frontend.Lexer;

namespace Frontend.AST
{
    public class ASTList : IASTNode
    {  
        private readonly ListPrototype prototype;

        private readonly List<IASTNode> _values;
        
        public ASTList(ListPrototype prototype, params IASTNode[] values)
        {
            this.prototype = prototype;
            _values = values.ToList();
        }

        public int Id()
            => prototype.Id();

        public IASTNode this[string name] 
            => _values[prototype.IdxOf(name)];

        public IEnumerable<IASTNode> Enumerate()
            => _values;

        public void Print(SymbolDictionary sd, string offset = "")
        {
            Console.WriteLine($"{offset}{prototype.Name()}:");
            for (var i = 0; i < _values.Count; i++)
            {
                Console.WriteLine($"{offset}   {i}:");
                _values[i].Print(sd, $"{offset}      ");
            }
        }

        public void Insert(IASTNode value)
        {
            _values.Insert(0, value);
        }
    }
}