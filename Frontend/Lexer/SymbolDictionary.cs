using System;
using System.Collections.Generic;

namespace Frontend.Lexer
{
    public class SymbolDictionary
    {
        private readonly Dictionary<(string, SymbolType), int> _idByName = new Dictionary<(string, SymbolType), int>(); 
        
        private readonly List<(string, SymbolType)> _nameById = new List<(string, SymbolType)>();

        public int Register(string name, SymbolType type)
        {
            if (_idByName.ContainsKey((name, type)))
                throw new Exception("Name already taken");
            _nameById.Add((name, type));
            _idByName[(name, type)] = _nameById.Count - 1;
            return _nameById.Count - 1;
        }

        public int RegisterTerminal(string name)
            => Register(name, SymbolType.Terminal);
        
        public int RegisterNonTerminal(string name)
            => Register(name, SymbolType.NonTerminal);
        
        public int RegisterComment(string name)
            => Register(name, SymbolType.Comment);

        public SymbolType TypeById(int id) 
            => this[id].symbolType;

        public bool ContainsKey(string name, SymbolType symbolType) => _idByName.ContainsKey((name, symbolType));
        
        public (string name, SymbolType symbolType) this[int id] => _nameById[id];
        
        public int this[string name, SymbolType symbolType] => _idByName[(name, symbolType)];
    }
}