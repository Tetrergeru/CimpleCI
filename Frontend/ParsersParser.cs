using System;
using System.Collections.Generic;
using System.Linq;
using Frontend.AST;
using Frontend.Lexer;
using Frontend.Parser;

namespace Frontend
{
    public static class ParsersParser
    {
        private static void AssertType(Token token, int type, string expected)
        {
            if (token.Id != type)
                throw new Exception($"Expected {expected}, but got {token.Text}, on line {token.Line}");
        }

        private static void AssertLength<T>(List<T> list, int position)
        {
            if (position >= list.Count)
                throw new Exception("Unexpected end of the file");
        }

        public static RegexLexer ParseLexer(string code)
        {
            var rules = new Dictionary<string, (string, bool)>();

            var sd = new SymbolDictionary();
            var lexerRules = new Dictionary<string, string>
            {
                ["Eq"] = "=",
                ["Colon"] = ";",
                ["Name"] = "[a-zA-Z_][a-zA-Z_0-9]*",
                ["Regex"] = "\".*\"",
            }.ToDictionary(kv => kv.Key, kv => (kv.Value, false));
            var lexer = new RegexLexer(lexerRules, sd);

            const SymbolType T = SymbolType.Terminal;
            var idEND = sd["END", T];
            var idEq = sd["Eq", T];
            var idColon = sd["Colon", T];
            var idName = sd["Name", T];
            var idRegex = sd["Regex", T];

            var tokens = lexer.ParseLexemes(code).ToList();
            for (var i = 0; i < tokens.Count && tokens[i].Id != idEND;)
            {
                var isComment = false;
                if (tokens[i].Id == idColon)
                    (isComment, i) = (true, i + 1);

                AssertLength(tokens, i + 2);
                AssertType(tokens[i], idName, "Name");
                AssertType(tokens[i + 1], idEq, "'='");
                AssertType(tokens[i + 2], idRegex, "Regex");

                var regex = tokens[i + 2].Text;
                rules[tokens[i].Text] = (regex.Substring(1, regex.Length - 2), isComment);
                i += 3;
            }

            return new RegexLexer(rules, new SymbolDictionary());
        }

        private class ASTParser
        {
            private RegexLexer lexer = new RegexLexer(new Dictionary<string, string>
            {
                ["OpenCurly"] = "\\{",
                ["CloseCurly"] = "\\}",
                ["Semicolon"] = ":",
                ["Colon"] = ";",
                ["Star"] = "\\*",
                ["Name"] = "[a-zA-Z_][a-zA-Z_0-9]*",
            }.ToDictionary(kv => kv.Key, kv => (kv.Value, false)), new SymbolDictionary());

            private readonly int _idOpenCurly, _idCloseCurly, _idSemicolon, _idColon, _idStar, _idName, _idEND;

            private int _i;
            
            private List<Token> _tokens;
            
            private readonly PrototypeDictionary _pd = new PrototypeDictionary();

            private readonly Dictionary<string, List<(string name, string type)>> _stringPrototypes
                = new Dictionary<string, List<(string name, string type)>>();

            public ASTParser()
            {
                _idOpenCurly = lexer["OpenCurly"];
                _idCloseCurly = lexer["CloseCurly"];
                _idSemicolon = lexer["Semicolon"];
                _idColon = lexer["Colon"];
                _idStar = lexer["Star"];
                _idName = lexer["Name"];
                _idEND = lexer["END"];
            }

            public PrototypeDictionary Parse(string code)
            {
                var obj = _pd.MakeNode("Object", null, 0);
                _pd.MakeNode("Token", obj, 0);
                _tokens = lexer.ParseLexemes(code).ToList();
                ParseAST();
                return _pd;
            }

            private void ParseAST()
            {
                for (; _i < _tokens.Count && _tokens[_i].Id != _idEND; _i++)
                    ParseNode();

                foreach (var (np, fields) in _stringPrototypes)
                {
                    var prototype = (NodePrototype) _pd.GetByName(np);
                    foreach (var (s, typeName) in fields)
                    {
                        var type = typeName[0] == '*'
                            ? _pd.GetOrMakeList(typeName)
                            : _pd.GetByName(typeName);
                        prototype.RegisterField(s, type);
                    }
                }

            }

            private void ParseNode()
            {
                var (node, parent) = ParseHeader();
                AssertLength(_tokens, _i + 2);
                AssertType(_tokens[_i + 1], _idOpenCurly, "'{'");
                _i += 2;
                var fields = new List<(string name, string type)>();
                while (_tokens[_i].Id != _idCloseCurly)
                    fields.Add(ParseField());

                _stringPrototypes[node] = fields;
                _pd.MakeNode(node, (NodePrototype) _pd.GetByName(parent), fields.Count);
            }

            private (string name, string parent) ParseHeader()
            {
                AssertLength(_tokens, _i + 2);
                AssertType(_tokens[_i], _idName, "Name");
                var name = _tokens[_i].Text;
                if (_tokens[_i + 1].Id != _idSemicolon)
                    return (name, "Object");

                AssertType(_tokens[_i + 2], _idName, "Name");
                var parent = _tokens[_i + 2].Text;
                _i += 2;

                return (name, parent);
            }

            private (string name, string type) ParseField()
            {
                AssertLength(_tokens, _i + 2);
                AssertType(_tokens[_i], _idName, "Name");
                AssertType(_tokens[_i + 1], _idSemicolon, "':'");
                var fName = _tokens[_i].Text;
                _i += 2;
                var stars = "";
                while (_tokens[_i].Id == _idStar)
                {
                    stars = $"{stars}*";
                    _i++;
                    AssertLength(_tokens, _i);
                }

                AssertLength(_tokens, _i + 2);

                AssertType(_tokens[_i], _idName, "Name");
                var fType = $"{stars}{_tokens[_i].Text}";

                AssertType(_tokens[_i + 1], _idColon, "';'");
                _i += 2;

                return (fName, fType);
            }
        }

        public static PrototypeDictionary ParseAST(string code)
            => new ASTParser().Parse(code);

        private class GrammarParser
        {
            private readonly RegexLexer _lexer = new RegexLexer(new Dictionary<string, string>
            {
                ["OpenCurly"] = "\\{",
                ["CloseCurly"] = "\\}",
                ["Dot"] = "\\.",
                ["Coma"]        = ",",
                ["Star"]        = "\\*",
                ["OpenPar"]     = "\\(",
                ["ClosePar"]    = "\\)",
                ["ArrowRight"] = "->",
                ["ArrowLeft"] = "<-",
                ["OpenAngle"] = "<",
                ["CloseAngle"]  = ">",
                ["DollarVar"] = "\\$[0-9]+",
                ["Colon"] = ";",
                ["VBar"] = "\\|",
                ["Return"] = "\\breturn\\b",
                ["Name"] = "[a-zA-Z_][a-zA-Z_0-9]*",
            }.ToDictionary(kv => kv.Key, kv => (kv.Value, false)), new SymbolDictionary());

            private readonly int
                _idOpenCurly,
                _idCloseCurly,
                _idDot,
                _idComa,
                _idStar,
                _idOpenAngle,
                _idCloseAngle,
                _idOpenPar,
                _idClosePar,
                _idArrowRight,
                _idArrowLeft,
                _idDollarVar,
                _idVBar,
                _idReturn,
                _idColon,
                _idName,
                _idEND;

            private int _i;
            private List<Token> _tokens;
            
            private RegexLexer _resultLexer;
            private SymbolDictionary _resultSD;
            private PrototypeDictionary _pd;

            private List<(string nt, List<(string name, SymbolType type)> seq, RuleCallback cb)> _rules =
                new List<(string, List<(string name, SymbolType type)>, RuleCallback)>();
            
            public GrammarParser(RegexLexer resultLexer, PrototypeDictionary pd)
            {
                _resultLexer = resultLexer;
                _resultSD = resultLexer.SymbolDictionary();
                _pd = pd;
                _idOpenCurly = _lexer["OpenCurly"];
                _idCloseCurly = _lexer["CloseCurly"];
                _idDot = _lexer["Dot"];
                _idComa = _lexer["Coma"];
                _idStar = _lexer["Star"];
                _idOpenAngle = _lexer["OpenAngle"];
                _idCloseAngle = _lexer["CloseAngle"];
                _idOpenPar = _lexer["OpenPar"];
                _idClosePar = _lexer["ClosePar"];
                _idArrowRight = _lexer["ArrowRight"];
                _idArrowLeft = _lexer["ArrowLeft"];
                _idDollarVar = _lexer["DollarVar"];
                _idColon = _lexer["Colon"];
                _idVBar = _lexer["VBar"];
                _idReturn = _lexer["Return"];
                _idName = _lexer["Name"];
                _idEND = _lexer["END"];
            }
            
            
            private void AssertType(Token token, int type, string expected)
            {
                if (token.Id != type)
                    throw new Exception($"Expected {expected}, but got {token.Text}, on line {token.Line}");
            }

            public RecursiveParser Parse(string code)
            {
                _tokens = _lexer.ParseLexemes(code).ToList();
                ParseGrammar();
                foreach (var (nt, _, _) in _rules)
                    if (!_resultSD.ContainsKey(nt, SymbolType.NonTerminal))
                        _resultSD.RegisterNonTerminal(nt);
                
                return new RecursiveParser(new Rules(_rules
                    .Select(nsc => new Rule(
                        _resultSD[nsc.nt, SymbolType.NonTerminal],
                        nsc.seq.Select(s => _resultSD[s.name, s.type]).ToList(),
                        nsc.cb)).ToList()), _resultSD);
            }

            private Token Consume(int type)
            {
                AssertLength(_tokens, _i);
                AssertType(_tokens[_i], type, _lexer[type]);
                _i++;
                return _tokens[_i - 1];
            }

            private Token Peek()
            {
                AssertLength(_tokens, _i);
                return _tokens[_i];
            }

            private void ParseGrammar()
            {
                while(_i < _tokens.Count && _tokens[_i].Id != _idEND)
                    ParseRule();
            }

            private void ParseRule()
            {
                var left = ParseNonTerminal();
                Consume(_idArrowRight);
                _rules.Add((left, ParseSequence(), ParseCallback()));
                while (Peek().Id == _idVBar)
                {
                    Consume(_idVBar);
                    _rules.Add((left, ParseSequence(), ParseCallback()));
                }
            }

            private List<(string name, SymbolType type)> ParseSequence()
            {
                var result = new List<(string name, SymbolType type)>();
                while(Peek().Id != _idOpenCurly)
                    result.Add(ParseSymbol());
                return result;
            }

            private (string name, SymbolType type) ParseSymbol()
                => Peek().Id == _idOpenAngle
                    ? (ParseNonTerminal(), SymbolType.NonTerminal)
                    : (Consume(_idName).Text, SymbolType.Terminal);

            private RuleCallback ParseCallback()
            {
                Consume(_idOpenCurly);
                var instr = new List<RuleCallback.Instruction>();
                while (Peek().Id != _idCloseCurly)
                {
                    if (Peek().Id == _idReturn)
                    {
                        Consume(_idReturn);
                        instr.Add(new RuleCallback.Return(ParseExpr()));
                    }
                    else
                    {
                        var left = ParseGetter();
                        Consume(_idArrowLeft);
                        var right = ParseExpr();
                        instr.Add(new RuleCallback.Add(left, right));
                    }

                    Consume(_idColon);
                }
                Consume(_idCloseCurly);
                return new RuleCallback(instr);;
            }

            private RuleCallback.Expr ParseExpr()
                => Peek().Id == _idDollarVar
                    ? (RuleCallback.Expr) ParseGetter()
                    : ParseConstruction();
            
            private RuleCallback.Getter ParseGetter()
            {
                var variable = int.Parse(Consume(_idDollarVar).Text[1..]);
                var gets = new List<string>();
                while (Peek().Id == _idDot)
                {
                    Consume(_idDot);
                    gets.Add(Consume(_idName).Text);
                }
                return new RuleCallback.Getter(variable, gets);
            }

            private RuleCallback.Construction ParseConstruction()
            {
                var type = ParseType();
                Consume(_idOpenPar);
                var exprList = new List<RuleCallback.Expr> ();
                if (Peek().Id == _idClosePar)
                {
                    Consume(_idClosePar);
                    return new RuleCallback.Construction(type, exprList);
                }

                exprList.Add(ParseExpr());
                while (Peek().Id != _idClosePar)
                {
                    Consume(_idComa);
                    exprList.Add(ParseExpr());
                }

                Consume(_idClosePar);
                return new RuleCallback.Construction(type, exprList);
            }

            private IPrototype ParseType()
            {
                var typeName = "";
                while (Peek().Id == _idStar)
                    typeName = $"{typeName}{Consume(_idStar).Text}";
                typeName = $"{typeName}{Consume(_idName).Text}";
                return typeName[0] == '*' 
                    ? _pd.GetOrMakeList(typeName)
                    : _pd.GetByName(typeName);
            }

            private string ParseNonTerminal()
            {
                Consume(_idOpenAngle);
                var ntName = Consume(_idName).Text;
                Consume(_idCloseAngle);
                return ntName;
            }
        }

        public static RecursiveParser ParseGrammar(string code, RegexLexer lexer, PrototypeDictionary pd)
            => new GrammarParser(lexer, pd).Parse(code);
    }
}