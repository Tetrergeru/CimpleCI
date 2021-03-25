using System;
using System.Collections.Generic;
using System.Linq;
using Frontend.AST;
using Frontend.Lexer;
using Frontend.Parser;
using Frontend.Parser.Ll1Parser;

namespace Frontend
{
    public class ParsersParser
    {
        private readonly ParsersLexer _lexer = new ParsersLexer();

        private List<Token> _code;

        private int _position;

        private bool CanPeek() => _position < _code.Count;

        private Token Peek() => _code[_position];

        private string TokenName(int id)
            => _lexer.SymbolDictionary[id].name;

        private string TokensNames(IEnumerable<int> tokens)
            => string.Join(", ", tokens.Select(TokenName));

        public (RegexLexer, PrototypeDictionary, Rules) ParseParser(string code)
        {
            _code = _lexer.ParseAll(code).ToList();
            var parsedLexer = ParseLexer();
            var pd = ParseAST();
            var parsedGrammar = ParseGrammar(parsedLexer.SymbolDictionary(), pd);
            Consume(_lexer.END);
            return (parsedLexer, pd, parsedGrammar);
        }

        /* ===== LEXER ===== */

        private RegexLexer ParseLexer()
        {
            Consume(_lexer.HashLex);
            var lexemes = new List<Lexeme>();
            while (CanPeek() && (Peek().Id == _lexer.Semicolon || Peek().Id == _lexer.Name))
            {
                var comment = false;
                if (Peek().Id == _lexer.Semicolon)
                {
                    comment = true;
                    Consume(_lexer.Semicolon);
                }

                var lexeme = ParseLexemeDeclaration();
                lexeme.Comment = comment;
                lexemes.Add(lexeme);
            }

            return new RegexLexer(lexemes, new SymbolDictionary());
        }

        private Lexeme ParseLexemeDeclaration()
        {
            var name = Consume(_lexer.Name);
            Consume(_lexer.Eq);
            var regex = Consume(_lexer.Regex);
            return new Lexeme(name.Text, ParseRegexToken(regex));
        }

        private string ParseRegexToken(Token regex)
            => regex.Text.Substring(1, regex.Text.Length - 2);

        /* ===== AST ===== */

        struct UnCompiledASTNode
        {
            public string Name;
            public string Parent;
            public List<(string name, string typeName)> Fields;

            public UnCompiledASTNode(string name, string parent, List<(string, string)> fields)
            {
                Name = name;
                Parent = parent;
                Fields = fields;
            }
        }

        private PrototypeDictionary ParseAST()
        {
            Consume(_lexer.HashAST);
            var pd = new PrototypeDictionary();

            var nodes = new List<UnCompiledASTNode>();
            while (Peek().Id == _lexer.Name)
                nodes.Add(ParseASTNode());


            var types = nodes
                .ToDictionary(
                    n => n.Name,
                    n => pd.MakeNode(n.Name, pd.GetByName(n.Parent) as NodePrototype, n.Fields.Count)
                );

            foreach (var node in nodes)
            foreach (var (name, typeName) in node.Fields)
                types[node.Name].RegisterField(name, pd.GetOrMakeByName(typeName));

            return pd;
        }

        private UnCompiledASTNode ParseASTNode()
        {
            var (name, parent) = ParseHeader();

            Consume(_lexer.LeftBrace);

            var fields = new List<(string name, string typeName)>();
            while (Peek().Id != _lexer.RightBrace)
                fields.Add(ParseField());

            Consume(_lexer.RightBrace);

            return new UnCompiledASTNode(name, parent, fields);
        }

        private (string name, string parentName) ParseHeader()
        {
            var name = Consume(_lexer.Name).Text;

            if (Peek().Id != _lexer.Colon)
                return (name, "Object");

            Consume(_lexer.Colon);
            var parentName = Consume(_lexer.Name).Text;
            return (name, parentName);
        }

        private (string name, string type) ParseField()
        {
            var name = Consume(_lexer.Name).Text;
            Consume(_lexer.Colon);
            var type = ParseTypeToString();
            Consume(_lexer.Semicolon);
            return (name, type);
        }

        private string ParseTypeToString()
        {
            var stars = 0;
            while (Peek().Id == _lexer.Star)
            {
                stars += 1;
                Consume(_lexer.Star);
            }

            var mainType = Consume(_lexer.Name).Text;
            return new string('*', stars) + mainType;
        }

        /* ===== GRAMMAR ===== */

        private Rules ParseGrammar(SymbolDictionary sd, PrototypeDictionary pd)
        {
            Consume(_lexer.HashGrammar);

            var rules = new List<Rule>();
            while (Peek().Id == _lexer.LeftAngle)
                rules.AddRange(ParseRule(sd, pd));

            return new Rules(rules);
        }

        private List<Rule> ParseRule(SymbolDictionary sd, PrototypeDictionary pd)
        {
            var rules = new List<Rule>();

            var left = ParseNonTerminal(sd);
            Consume(_lexer.ArrowRight);
            rules.Add(new Rule(left, ParseSequence(sd), ParseCallback(pd)));
            while (Peek().Id == _lexer.Bar)
            {
                Consume(_lexer.Bar);
                rules.Add(new Rule(left, ParseSequence(sd), ParseCallback(pd)));
            }

            return rules;
        }

        private List<int> ParseSequence(SymbolDictionary sd)
        {
            var result = new List<int>();
            while (Peek().Id != _lexer.LeftBrace)
                result.Add(ParseSymbol(sd));
            return result;
        }

        private int ParseSymbol(SymbolDictionary sd)
            => Peek().Id == _lexer.LeftAngle
                ? ParseNonTerminal(sd)
                : sd[Consume(_lexer.Name).Text, SymbolType.Terminal];

        private int ParseNonTerminal(SymbolDictionary sd)
        {
            Consume(_lexer.LeftAngle);
            var ntName = Consume(_lexer.Name).Text;
            Consume(_lexer.RightAngle);
            return sd.GetOrRegister(ntName, SymbolType.NonTerminal);
        }

        private RuleCallback ParseCallback(PrototypeDictionary pd)
        {
            Consume(_lexer.LeftBrace);
            var instructions = new List<RuleCallback.Instruction>();
            while (Peek().Id != _lexer.RightBrace)
            {
                if (Peek().Id == _lexer.Return)
                {
                    Consume(_lexer.Return);
                    instructions.Add(new RuleCallback.Return(ParseExpression(pd)));
                }
                else
                {
                    var left = ParseGetter();
                    Consume(_lexer.ArrowLeft);
                    var right = ParseExpression(pd);
                    instructions.Add(new RuleCallback.Add(left, right));
                }

                Consume(_lexer.Semicolon);
            }

            Consume(_lexer.RightBrace);
            return new RuleCallback(instructions);
        }

        private RuleCallback.Expr ParseExpression(PrototypeDictionary pd)
            => Peek().Id == _lexer.DollarVar
                ? (RuleCallback.Expr) ParseGetter()
                : ParseConstruction(pd);

        private RuleCallback.Getter ParseGetter()
        {
            var variable = int.Parse(Consume(_lexer.DollarVar).Text[1..]);
            var gets = new List<string>();
            while (Peek().Id == _lexer.Dot)
            {
                Consume(_lexer.Dot);
                gets.Add(Consume(_lexer.Name).Text);
            }

            return new RuleCallback.Getter(variable, gets);
        }

        private RuleCallback.Construction ParseConstruction(PrototypeDictionary pd)
        {
            var type = ParseType(pd);
            Consume(_lexer.LeftPar);
            var exprList = new List<RuleCallback.Expr>();
            if (Peek().Id == _lexer.RightPar)
            {
                Consume(_lexer.RightPar);
                return new RuleCallback.Construction(type, exprList);
            }

            exprList.Add(ParseExpression(pd));
            while (Peek().Id != _lexer.RightPar)
            {
                Consume(_lexer.Coma);
                exprList.Add(ParseExpression(pd));
            }

            Consume(_lexer.RightPar);
            return new RuleCallback.Construction(type, exprList);
        }

        private IPrototype ParseType(PrototypeDictionary pd)
        {
            var stars = "";
            while (Peek().Id == _lexer.Star)
                stars += Consume(_lexer.Star).Text;
            var mainType = Consume(_lexer.Name).Text;
            return stars.Length > 0 ? pd.GetOrMakeList(stars + mainType) : pd.GetByName(mainType);
        }

        private Token Consume(params int[] type)
        {
            if (!CanPeek())
                throw new Exception($"Expected any of {{{TokensNames(type)}}}, got EOF");
            var peek = Peek();
            if (!type.Contains(peek.Id))
                throw new Exception(
                    $"Expected any of {{{TokensNames(type)}}}, got {TokenName(peek.Id)} on line {peek.Line}");
            _position++;
            return peek;
        }
    }

    public class ParsersLexer
    {
        public IEnumerable<Token> ParseAll(string code)
            => _lexer.ParseLexemes(code);


        public SymbolDictionary SymbolDictionary;

        private RegexLexer _lexer = new RegexLexer(
            new List<Lexeme>
            {
                new Lexeme("Space", "\\s", true),
                new Lexeme("Comment", "/\\*.*\\*/", true),
                new Lexeme("HashLex", "#Lex"),
                new Lexeme("HashAST", "#AST"),
                new Lexeme("HashGrammar", "#Grammar"),
                new Lexeme("Dot", "\\."),
                new Lexeme("DollarVar", "\\$[0-9]+"),
                new Lexeme("Eq", "="),
                new Lexeme("Star", "\\*"),
                new Lexeme("LeftBrace", "\\{"),
                new Lexeme("RightBrace", "\\}"),
                new Lexeme("LeftPar", "\\("),
                new Lexeme("RightPar", "\\)"),
                new Lexeme("Semicolon", ";"),
                new Lexeme("Colon", ":"),
                new Lexeme("Coma", ","),
                new Lexeme("ArrowRight", "->"),
                new Lexeme("ArrowLeft", "<-"),
                new Lexeme("LeftAngle", "<"),
                new Lexeme("RightAngle", ">"),
                new Lexeme("Bar", "\\|"),
                new Lexeme("Return", "\\breturn\\b"),
                new Lexeme("Name", "[a-zA-Z_][a-zA-Z_0-9]*"),
                new Lexeme("Regex", "\"([^\\\"]|\\.)*\""),
            },
            new SymbolDictionary()
        );

        public int HashLex,
            HashAST,
            HashGrammar,
            Dot,
            DollarVar,
            Eq,
            Star,
            LeftBrace,
            RightBrace,
            LeftPar,
            RightPar,
            Semicolon,
            Colon,
            Coma,
            ArrowRight,
            ArrowLeft,
            LeftAngle,
            RightAngle,
            Bar,
            Return,
            Name,
            Regex,
            END;

        public ParsersLexer()
        {
            foreach (var field in typeof(ParsersLexer).GetFields())
            {
                if (field.FieldType != typeof(int))
                    continue;
                field.SetValue(this, _lexer[field.Name]);
            }

            END = _lexer["END"];

            SymbolDictionary = _lexer.SymbolDictionary();
        }
    }
}