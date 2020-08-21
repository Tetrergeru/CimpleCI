﻿using System.Linq;
using Frontend.AST;
using Frontend.Lexer;
using Frontend.Parser;

namespace Frontend
{
    public class FrontendPipeline
    {
        private readonly RegexLexer _lexer;
        private readonly SymbolDictionary _symbolDictionary;
        private readonly PrototypeDictionary _prototypeDictionary;
        private readonly RecursiveParser _parser;

        public FrontendPipeline(string grammar)
        {
            var parsedGrammar = grammar.Split("#Lex")
                .SelectMany(s => s.Split("#AST").SelectMany(s => s.Split("#Grammar"))).ToList();
            
            _lexer = ParsersParser.ParseLexer(parsedGrammar[1]);
            _symbolDictionary = _lexer.SymbolDictionary();
            _prototypeDictionary = ParsersParser.ParseAST(parsedGrammar[2]);
            _parser = ParsersParser.ParseGrammar(parsedGrammar[3], _lexer, _prototypeDictionary);
        }

        public IASTNode Parse(string code)
            => _parser.Parse(_lexer.ParseLexemes(code).ToList());
    }
}