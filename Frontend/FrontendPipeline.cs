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
        public readonly RegexLexer Lexer;
        public readonly SymbolDictionary SymbolDictionary;
        public readonly PrototypeDictionary PrototypeDictionary;
        public readonly IParser<IASTNode> Parser;

        public FrontendPipeline(string grammar)
        {
            Rules<IASTNode> rules;
            (Lexer, PrototypeDictionary, rules) = new ParsersParser1().ParseParser(grammar);
            SymbolDictionary = Lexer.SymbolDictionary();
            Parser = new Ll1Parser<IASTNode>(rules, SymbolDictionary, (token, id) => new ASTLeaf(token, id));
        }

        public IASTNode Parse(string code)
            => Parser.Parse(Lexer.ParseLexemes(code).ToList());

        public void Print(IASTNode node)
        {
            if (node == null)
            {
                Console.WriteLine("NONE");
                return;
            }

            node.Print(SymbolDictionary);
        }
    }
}