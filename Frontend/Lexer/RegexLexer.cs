using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Frontend.Parser;

namespace Frontend.Lexer
{
    public struct Lexeme
    {
        public string Name, Regex;
        public bool Comment;

        public Lexeme(string name, string regex, bool comment = false)
        {
            Name = name;
            Regex = regex;
            Comment = comment;
        }
    }

    public class RegexLexer
    {
        public static bool DEBUG = false;

        private readonly Regex _regex;

        private readonly SymbolDictionary _symbolDictionary;

        public int this[string terminalName]
            => _symbolDictionary[terminalName, SymbolType.Terminal];

        public string this[int terminalId]
            => _symbolDictionary[terminalId].name;

        private int _line = 0;

        public RegexLexer(List<Lexeme> rules, SymbolDictionary symbolDictionary)
            : this(rules.Select(r => (r.Name, (r.Regex, r.Comment))).ToList(), symbolDictionary)
        {
        }

        public RegexLexer(List<(string terminal, (string code, bool isComment) code)> rules,
            SymbolDictionary symbolDictionary)
        {
            _symbolDictionary = symbolDictionary;
            foreach (var (name, (_, isComment)) in rules)
                _symbolDictionary.GetOrRegister(name, isComment ? SymbolType.Comment : SymbolType.Terminal);
            _symbolDictionary.GetOrRegister("END", SymbolType.Terminal);


            var strRules = string.Join("|",
                rules
                    .Select(kv => $"(?<{kv.terminal}>{kv.code.code})"));

            _regex = new Regex($"{strRules}|.");
        }

        private Token ParseMatch(Match match)
        {
            var text = match.ToString();
            _line += text.Count(ch => ch == '\n');

            var groupKey = match.Groups.Keys.Where(g => !char.IsDigit(g[0]) && match.Groups[g].Success)
                .FirstOrDefault(_ => true);


            if (groupKey == null)
                throw new Exception($"Unexpected token '{text}' on line {_line}");

            var result = _symbolDictionary.ContainsKey(groupKey, SymbolType.Comment)
                ? null
                : new Token(this[groupKey], match.ToString(), _line);

            if (DEBUG && result != null)
                Console.WriteLine($"{this[result.Id]}: {result.Text}");

            return result;
        }

        public IEnumerable<Token> ParseLexemes(string code)
        {
            return _regex
                .Matches(code)
                .Select(ParseMatch)
                .Where(token => token != null)
                .Append(new Token(_symbolDictionary["END", SymbolType.Terminal], "\0", _line));
        }

        public SymbolDictionary SymbolDictionary() => _symbolDictionary;
    }
}