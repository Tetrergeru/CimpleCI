using System;
using System.Collections.Generic;
using System.Linq;

namespace Frontend.AST
{
    public class NodePrototype : IPrototype
    {
        private NodePrototype _parent;

        private int _id;
        
        private string _name;

        private readonly Dictionary<string, (int index, IPrototype type)> _fields =
            new Dictionary<string, (int index, IPrototype type)>();

        private readonly int _fieldNumber;

        private int _currentField;

        public NodePrototype(int id, string name, NodePrototype parent, int fieldNumber)
        {
            _id = id;
            _name = name;
            _parent = parent;
            _currentField = _parent?._fieldNumber ?? 0;
            _fieldNumber = fieldNumber + _currentField;
        }

        public void RegisterField(string name, IPrototype type)
        {
            _currentField++;
            if (_currentField + _parent._fieldNumber > _fieldNumber)
                throw new Exception($"More fields, than expected. \"{name}\" is the problem");
            _fields[name] = (_currentField - 1, type);
        }

        public int Id()
            => _id;

        public string Name()
            => _name;

        public IASTNode New(params IASTNode[] args)
        {
            if (args.Length != _fieldNumber)
                throw new Exception($"Wrong argument number: expected {_fieldNumber}, but got {args.Length}");
            return new ASTObject(this, args);
        }

        public int IdxOf(string name)
        {
            if (_fields.ContainsKey(name))
                return _fields[name].index;
            if (_parent == null)
                throw new Exception($"No such field, as {name}");
            return _parent.IdxOf(name);
        }

        public bool Is(IPrototype other)
            => Id() == other.Id() || _parent != null && _parent.Is(other);

        public IEnumerable<string> Names()
            => _fields.Select(f => f.Key);
    }
}