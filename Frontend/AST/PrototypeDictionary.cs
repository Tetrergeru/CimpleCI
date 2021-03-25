using System;
using System.Collections.Generic;
using System.Linq;

namespace Frontend.AST
{
    public class PrototypeDictionary
    {
        private readonly Dictionary<string, int> _idByName = new Dictionary<string, int>(); 
        
        private readonly List<(string name, IPrototype type)> _nameById = new List<(string, IPrototype)>();

        public readonly NodePrototype Object;
        public readonly NodePrototype Token;
        
        public PrototypeDictionary()
        {
            Object = MakeNode("Object", null, 0);
            Token = MakeNode("Token", Object, 0);
        }

        public int Register(string name, IPrototype prototype)
        {
            if (_idByName.ContainsKey(name))
                throw new Exception("Name already taken");
            _nameById.Add((name, prototype));
            _idByName[name] = _nameById.Count - 1;
            return _nameById.Count - 1;
        }

        public NodePrototype MakeNode(string name, NodePrototype parent, int fieldNumber)
        {
            var np = new NodePrototype(_nameById.Count, name, parent, fieldNumber);
            Register(name, np);
            return np;
        }

        public ListPrototype GetOrMakeList(string name)
            => _idByName.ContainsKey(name)
                ? (ListPrototype) GetByName(name)
                : MakeList(name);

        public ListPrototype MakeList(string name)
        {
            if (_idByName.ContainsKey(name))
                throw new Exception("Name already taken");
            if (!_idByName.ContainsKey(name[1..]) && name[1] == '*')
                MakeList(name[1..]);
            var lp = new ListPrototype(_nameById.Count, GetByName(name[1..]));
            Register(name, lp);
            return lp;
        }

        public int this[string name]
            => _idByName[name];
        
        public IPrototype GetByName(string name)
            => _nameById[this[name]].type;

        public IPrototype GetOrMakeByName(string name)
            => name.StartsWith("*") ? GetOrMakeList(name) : GetByName(name);
    }
}