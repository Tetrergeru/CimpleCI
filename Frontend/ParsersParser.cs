using System;
using System.Collections.Generic;
using System.Linq;
using Frontend.AST;
using Frontend.Lexer;
using Frontend.Parser;
using Frontend.Parser.Ll1Parser;

namespace Frontend
{
    public class ParsersLexer
    {
        public IEnumerable<Token> ParseAll(string code)
            => _lexer.ParseLexemes(code);

        public readonly SymbolDictionary SymbolDictionary;

        private readonly RegexLexer _lexer = new RegexLexer(
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

    public class ParsersParser1
    {
        private PrototypeDictionary pd;
        private SymbolDictionary resultSd;
        private RegexLexer resultLexer;

        private ParsersLexer pl = new ParsersLexer();
        private Ll1Parser<object> pParser;

        public (RegexLexer, PrototypeDictionary, Rules<IASTNode>) ParseParser(string code)
        {
            pd = new PrototypeDictionary();
            resultSd = new SymbolDictionary();
            var tokens = pl.ParseAll(code).ToList();
            return ((RegexLexer, PrototypeDictionary, Rules<IASTNode>)) pParser.Parse(tokens);
        }

        public int Parser,
            LexemeList,
            Lexer,
            Lexeme,
            Ast,
            AstNode,
            Fields,
            Field,
            FieldType,
            Grammar,
            Rule,
            NonTerminal,
            RuleRights,
            RuleRight,
            SymbolSequence,
            Callback,
            Symbol,
            Statements,
            Statement,
            Expression,
            Getters,
            ExprList;

        public ParsersParser1()
        {
            var sd = pl.SymbolDictionary;
            foreach (var field in typeof(ParsersParser1).GetFields())
                if (field.FieldType == typeof(int))
                    field.SetValue(this, sd.RegisterNonTerminal(field.Name));

            pParser = new Ll1Parser<object>(InitRules(), sd, (token, id) => token.Text);
        }

        // @formatter:off
        private Rules<object> InitRules()
        {
            return new Rules<object>(new List<Rule<object>>
            {
                R(Parser, /**/ pl.HashLex, Lexer, pl.HashAST, Ast, pl.HashGrammar, Grammar, pl.END).C(l => ((RegexLexer)l[1], FinalizeAst(l[3]), FinalizeGrammar(l[5]))),
                
                R(Lexer,      /**/ LexemeList                            ).C(l => FinalizeLexer(l[0])),
                R(LexemeList, /**/ Lexeme, LexemeList                    ).ArrayAdd<object>(1, 0),
                R(LexemeList  /**/                                       ).C(l => new List<object>()),
                R(Lexeme,     /**/ pl.Name, pl.Eq, pl.Regex              ).C(l => MakeLexeme(l[0], l[2], SymbolType.Terminal)),
                R(Lexeme,     /**/ pl.Semicolon, pl.Name, pl.Eq, pl.Regex).C(l => MakeLexeme(l[1], l[3], SymbolType.Comment)),
                
                R(Ast,       /**/ AstNode, Ast                                                   ).ArrayAdd<object>(1, 0),
                R(Ast        /**/                                                                ).C(l => new List<object>()),
                R(AstNode,   /**/ pl.Name, pl.LeftBrace, Fields, pl.RightBrace                   ).C(l => MakeNode(l[0], "Object", l[2])),
                R(AstNode,   /**/ pl.Name, pl.Colon, pl.Name, pl.LeftBrace, Fields, pl.RightBrace).C(l => MakeNode(l[0], l[2], l[4])),
                R(Fields,    /**/ Field, Fields                                                  ).ArrayAdd<object>(1, 0),
                R(Fields     /**/                                                                ).MakeArray<object>(),
                R(Field,     /**/ pl.Name, pl.Colon, FieldType, pl.Semicolon                     ).C(l => (l[0] as string, l[2] as string)),
                R(FieldType, /**/ pl.Name                                                        ).C(l => l[0] as string),
                R(FieldType, /**/ pl.Star, pl.Name                                               ).C(l => '*' + (l[1] as string)),
                
                R(Grammar,        /**/ Rule, Grammar                         ).ArrayAdd<object>(1, 0),
                R(Grammar         /**/                                       ).MakeArray<object>(),
                R(Rule,           /**/ NonTerminal, pl.ArrowRight, RuleRights).C(l => FinalizeRule(l[0], l[2])),
                R(RuleRights,     /**/ RuleRight, pl.Bar, RuleRights         ).ArrayAdd<object>(2, 0),
                R(RuleRights,     /**/ RuleRight                             ).MakeArray<object>(0),
                R(RuleRight,      /**/ SymbolSequence, Callback              ).C(l => ( (List<int>)l[0], (RuleCallback)l[1] )),
                R(SymbolSequence, /**/ Symbol, SymbolSequence                ).ArrayAdd<int>(1, 0),
                R(SymbolSequence  /**/                                       ).MakeArray<int>(),
                R(Symbol,         /**/ NonTerminal                           ).C(l => l[0]),
                R(Symbol,         /**/ pl.Name                               ).C(l => resultSd[(string)l[0], SymbolType.Terminal]),
                R(Symbol,         /**/ pl.Regex                              ).C(l => resultLexer.MatchOneToken(ParseRegexToken(l[0]))),
                R(NonTerminal,    /**/ pl.LeftAngle, pl.Name, pl.RightAngle  ).C(l => resultSd.GetOrRegister((string)l[1], SymbolType.NonTerminal)),
                
                R(Callback,   /**/ pl.LeftBrace, Statements, pl.RightBrace           ).C(l => new RuleCallback((List<RuleCallback.Instruction>) l[1])),  
                R(Statements, /**/ Statement, Statements                             ).ArrayAdd<RuleCallback.Instruction>(1, 0),
                R(Statements  /**/                                                   ).MakeArray<RuleCallback.Instruction>(),
                R(Statement,  /**/ pl.Return, Expression, pl.Semicolon               ).C(l => NewReturn(l[1])),
                R(Statement,  /**/ Expression, pl.ArrowLeft, Expression, pl.Semicolon).C(l => NewAdd(l[0], l[2])),
                R(Expression, /**/ pl.DollarVar, Getters                             ).C(l => NewGetter(l[0], l[1])),
                R(Expression, /**/ FieldType, pl.LeftPar, ExprList, pl.RightPar      ).C(l => NewConstructor(l[0], l[2])),
                R(Getters,    /**/ pl.Dot, pl.Name, Getters                          ).ArrayAdd<string>(2, 1),
                R(Getters     /**/                                                   ).MakeArray<string>(),
                R(ExprList,   /**/ Expression, pl.Coma, ExprList                     ).ArrayAdd<RuleCallback.Expr>(2, 0),
                R(ExprList,   /**/ Expression                                        ).MakeArray<RuleCallback.Expr>(0),
                R(ExprList    /**/                                                   ).MakeArray<RuleCallback.Expr>(),
            });
        }
        // @formatter:on

        private static string ParseRegexToken(object regex)
        {
            return ((string) regex).Substring(1, ((string) regex).Length - 2);
        }

        private object MakeNode(object name, object parentName, object fields)
            => (
                pd.MakeNode(name as string, ((List<object>) fields).Count),
                parentName as string,
                (List<object>) fields
            );

        private object MakeLexeme(object name, object regex, SymbolType type)
            => new Lexeme((string) name, ParseRegexToken((string) regex), type == SymbolType.Comment);

        private static RuleCallback.Return NewReturn(object expr)
            => new RuleCallback.Return(expr as RuleCallback.Expr);

        private static RuleCallback.Add NewAdd(object list, object expr)
            => new RuleCallback.Add(list as RuleCallback.Getter, expr as RuleCallback.Expr);

        private static RuleCallback.Getter NewGetter(object arg, object fields)
            => new RuleCallback.Getter(int.Parse(((string) arg).Substring(1)), (List<string>) fields);

        private RuleCallback.Construction NewConstructor(object type, object expr)
            => new RuleCallback.Construction(pd.GetOrMakeByName(type as string), expr as List<RuleCallback.Expr>);

        private PrototypeDictionary FinalizeAst(object astObj)
        {
            var ast = ((List<object>) astObj).Select(o =>
                ((NodePrototype prototype, string parent, List<object> fields)) o);

            foreach (var (prototype, parent, fields) in ast)
            {
                prototype.ChangeParent((NodePrototype) pd.GetByName(parent));
                foreach (var (name, type) in fields.Select(f => ((string name, string type)) f))
                    prototype.RegisterField(name, pd.GetOrMakeByName(type));
            }

            return pd;
        }

        private RegexLexer FinalizeLexer(object lexerObj)
            => resultLexer = new RegexLexer(
                ((List<object>) lexerObj).Select(o => (Lexeme) o).ToList(), resultSd);

        private List<Rule<IASTNode>> FinalizeRule(object nonTerminal, object rights)
            => ((List<object>) rights)
                .Select(right => ((List<int> seq, RuleCallback cb)) right)
                .Select(right =>
                    new Rule<IASTNode>((int) nonTerminal, right.seq, right.cb))
                .ToList();

        private Rules<IASTNode> FinalizeGrammar(object rules)
            => new Rules<IASTNode>(
                ((List<object>) rules)
                .SelectMany(rule => (List<Rule<IASTNode>>) rule)
                .ToList()
            );

        private static RulePrototype R(int nt, params int[] sequence)
            => new RulePrototype(nt, sequence);
    }

    internal class RulePrototype
    {
        private readonly int _nt;
        private readonly IReadOnlyList<int> _seq;

        public RulePrototype(int nt, IReadOnlyList<int> seq)
        {
            _nt = nt;
            _seq = seq;
        }

        public Rule<object> C(Func<IReadOnlyList<object>, object> lambda)
            => new Rule<object>(_nt, _seq, new Callback(lambda));

        public Rule<object> ArrayAdd<T>(int array, int data)
            => C(l =>
            {
                ((List<T>) l[array]).Insert(0, (T) l[data]);
                return l[array];
            });

        public Rule<object> MakeArray<T>() => C(l => new List<T>());

        public Rule<object> MakeArray<T>(int i) => C(l => new List<T> {(T) l[i]});
    }
}