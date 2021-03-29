using System;
using System.Collections.Generic;
using Frontend.AST;
using Frontend.Lexer;

namespace Frontend.Parser
{
    public class RecursiveParser<T> : IParser
    {
        private readonly Rules<T> _rules;

        private readonly SymbolDictionary _symbolDictionary;

        private List<Token> _code;
        
        public RecursiveParser(Rules<T> rules, SymbolDictionary symbolDictionary)
        {
            _symbolDictionary = symbolDictionary;
            _rules = rules;
        }

        private IASTNode Parse(int position, int rule, out int newPosition)
        {
            var list = new List<IASTNode>();
            foreach (var token in _rules.RuleList[rule].Sequence)
            {
                if (_symbolDictionary.TypeById(token) == SymbolType.Terminal)
                {
                    if (position < _code.Count && token == _code[position].Id)
                    {
                        // =============== TODO: Fix id ===============
                        
                        list.Add(new ASTLeaf(_code[position], 1));
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

            if (RegexLexer.DEBUG)
            {
                if (result != null)
                    result.Print(_symbolDictionary);
                else
                    Console.WriteLine();

                Console.WriteLine("============================================================================");
            }

            return result;
        }

        public IASTNode Parse(List<Token> code)
        {
            _code = code;
            return Parse(0, 0, out _);
        }
    }
}