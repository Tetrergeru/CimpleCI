using System;

namespace Frontend.AST
{
    public class ListPrototype : IPrototype
    {
        private int _id;

        public readonly IPrototype Type;

        public ListPrototype(int id, IPrototype type)
        {
            _id = id;
            Type = type;
        }

        public string Name()
            => $"*{Type.Name()}"; 
        
        public int Id()
            => _id;

        public IASTNode New(params IASTNode[] args)
            => new ASTList(this, args);

        public int IdxOf(string name)
        {
            if (!int.TryParse(name, out var result))
                throw new Exception($"Expected number, as ASTList field, but got {name}");
            return result;
        }

        public bool Is(IPrototype other) 
            => _id == other.Id();
    }
}