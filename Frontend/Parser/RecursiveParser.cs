using System;
using System.Collections.Generic;
using Frontend.AST;
using Frontend.Lexer;

namespace Frontend.Parser
{
    public class RecursiveParser<T> : IParser<T> where T : class
    {
        private readonly Rules<T> _rules;

        private readonly SymbolDictionary _symbolDictionary;

        private List<Token> _code;
        
        private readonly Func<Token, int, T> _factory;
        
        public RecursiveParser(Rules<T> rules, SymbolDictionary symbolDictionary, Func<Token, int, T> factory)
        {
            _symbolDictionary = symbolDictionary;
            _factory = factory;
            _rules = rules;
        }

        private T Parse(int position, int rule, out int newPosition)
        {
            var list = new List<T>();
            foreach (var token in _rules.RuleList[rule].Sequence)
            {
                if (_symbolDictionary.TypeById(token) == SymbolType.Terminal)
                {
                    if (position < _code.Count && token == _code[position].Id)
                    {
                        // =============== TODO: Fix id ===============
                        
                        list.Add(_factory(_code[position], 1));
                        position++;
                        continue;
                    }

                    newPosition = -1;
                    return null;
                }

                var success = false;
                foreach (var ntRule in _rules.RuleGroup(token))
                {
                    var parsed = Parse(position, ntRule, out var newPos);
                    if (parsed == null)
                        continue;

                    success = true;
                    list.Add(parsed);
                    position = newPos;
                    
                    break;
                }

                if (success) 
                    continue;
                
                newPosition = -1;
                return null;
            }
            
            newPosition = position;
            var result = _rules[rule].Callback.Call(list);


            return result;
        }

        public T Parse(List<Token> code)
        {
            _code = code;
            return Parse(0, 0, out _);
        }
    }
}