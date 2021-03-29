using System;
using System.Linq;
using Frontend.AST;
using Frontend.Lexer;
using Frontend.Parser;
using Frontend.Parser.Ll1Parser;

namespace Frontend
{
    public class FrontendPipeline
    {
        private readonly RegexLexer _lexer;
        private readonly SymbolDictionary _symbolDictionary;
        private readonly PrototypeDictionary _prototypeDictionary;
        private readonly IParser _parser;

        public FrontendPipeline(string grammar)
        {
            Rules<IASTNode> rules;
            (_lexer, _prototypeDictionary, rules) = new ParsersParser().ParseParser(grammar);
            _symbolDictionary = _lexer.SymbolDictionary();
            _parser = new Ll1Parser<IASTNode>(rules, _symbolDictionary);
        }

        public IASTNode Parse(string code)
            => _parser.Parse(_lexer.ParseLexemes(code).ToList());

        public void Print(IASTNode node)
        {
            if (node == null)
            {
                Console.WriteLine("NONE");
                return;
            }

            node.Print(_symbolDictionary);
        }
    }
}