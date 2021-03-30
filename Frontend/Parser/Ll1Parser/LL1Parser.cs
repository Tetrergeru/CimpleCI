using System;
using System.Collections.Generic;
using System.Linq;
using Frontend.AST;
using Frontend.Lexer;

namespace Frontend.Parser.Ll1Parser
{
    public class Ll1Parser<T> : IParser<T>
    {
        public T Parse(List<Token> code)
        {
            _code = code;
            return ConsumeNonTerminal(_rules[0].NonTerminal);
        }

        private readonly SymbolDictionary _symbolDictionary;

        private List<Token> _code;

        private int _position;

        private Token Peek() => _code[_position];

        private readonly Rules<T> _rules;

        private readonly Dictionary<int, HashSet<int>> _first;

        private readonly Dictionary<int, IConsumer<T>> _consumers;

        private readonly Func<Token, int, T> _factory;
        
        public Ll1Parser(Rules<T> rules, SymbolDictionary symbolDictionary, Func<Token, int, T> factory)
        {
            _symbolDictionary = symbolDictionary;
            this._factory = factory;
            _rules = rules;
            _first = CalculateFirst();
            _consumers = _symbolDictionary.GetAll(SymbolType.NonTerminal)
                .ToDictionary(x => x, MakeConsumer);
        }

        private T ConsumeToken(int type)
        {
            if (Peek().Id != type)
                throw new Exception($"Expected {_symbolDictionary[type].name}, got {Peek().Text} on line {Peek().Line}");

            return _factory(_code[_position++], type);
        }

        private T ConsumeNonTerminal(int type)
        {
            var output = new List<T>();
            var consumer = _consumers[type];
            return UseConsumer(consumer, output);
        }

        private T UseConsumer(IConsumer<T> consumer, List<T> output)
        {
            while (true)
            {
                switch (consumer)
                {
                    case FinalConsumer<T> fc:
                        return _rules[fc.Rule].Callback.Call(output);
                    case SymbolConsumer<T> syc:
                    {
                        output.Add(syc.Action());
                        consumer = syc.Consumer;
                        continue;
                    }
                    case SwitchConsumer<T> sc:
                    {
                        var peek = Peek();
                        var c = sc.Go(peek.Id);

                        consumer = c ??
                                   throw new Exception(
                                       $@"Expected one of {
                                               string
                                                   .Join(", ",
                                                       sc.Start()
                                                           .Select(k => _symbolDictionary[k].name))
                                           }, got {_symbolDictionary[peek.Id].name}({peek.Text}), on line {peek.Line}");
                        continue;
                    }
                    default:
                        throw new Exception("Unknown consumer");
                }
            }
        }

        private IConsumer<T> MakeConsumer(int nonTerminal)
            => MakeConsumer(_rules.RuleGroup(nonTerminal), 0);

        private IConsumer<T> MakeConsumer(IReadOnlyList<int> rules, int position)
        {
            var nt = _rules[rules[0]].NonTerminal;
            var list = new List<IConsumer<T>>();

            var longRules = rules.Where(r => _rules[r].Sequence.Count > position);
            foreach (var ruleGroup in longRules.GroupBy(r => _rules[r].Sequence[position]))
            {
                if (list.Any(c => c.CanConsume(ruleGroup.Key)))
                    throw new Exception(
                        $"Ambiguity: cannot decide what to consume for non terminal <{_symbolDictionary[nt].name}>, position = {position}");

                var symbol = ruleGroup.Key;
                var nextConsumer = MakeConsumer(ruleGroup.ToList(), position + 1);
                list.Add(new SymbolConsumer<T>(
                        _first[symbol],
                        _symbolDictionary[ruleGroup.Key].symbolType switch
                        {
                            SymbolType.Terminal => () => ConsumeToken(symbol),
                            SymbolType.NonTerminal => () => ConsumeNonTerminal(symbol),
                            _ => throw new Exception($"Unexpected symbol type {_symbolDictionary[ruleGroup.Key].symbolType}")
                        },
                        nextConsumer
                    )
                );
            }

            var finishedRules = rules.Where(r => _rules[r].Sequence.Count == position).ToList();
            if (finishedRules.Count > 1)
                throw new Exception(
                    $"Ambiguity: cannot decide which rule finishes for non terminal <{_symbolDictionary[nt].name}>");
            if (finishedRules.Count > 0)
                list.Add(new FinalConsumer<T>(finishedRules[0]));

            if (list.Count == 0)
                throw new Exception(
                    $"Don't know what to parse for non terminal <{_symbolDictionary[nt].name}>, position = {position}");
            return list.Count == 1 ? list[0] : new SwitchConsumer<T>(list);
        }

        private Dictionary<int, HashSet<int>> CalculateFirst()
        {
            var first = _symbolDictionary.GetAll(SymbolType.NonTerminal)
                .ToDictionary(nt => nt, _ => new HashSet<int>());
            foreach (var t in _symbolDictionary.GetAll(SymbolType.Terminal))
                first[t] = new HashSet<int> {t};
            var changed = true;

            void FirstInsert(int nonTerminal, int term)
            {
                if (first[nonTerminal].Contains(term)) return;
                first[nonTerminal].Add(term);
                changed = true;
            }

            while (changed)
            {
                changed = false;
                foreach (var nonTerminal in _symbolDictionary.GetAll(SymbolType.NonTerminal))
                {
                    foreach (var r in _rules.FullRuleGroup(nonTerminal))
                    {
                        if (r.Sequence.Count == 0)
                            continue;
                        var r1 = r.Sequence.First();
                        switch (_symbolDictionary[r1].symbolType)
                        {
                            case SymbolType.Terminal:
                                FirstInsert(nonTerminal, r.Sequence.First());
                                break;
                            case SymbolType.NonTerminal:
                            {
                                foreach (var term in first[r1])
                                    FirstInsert(nonTerminal, term);
                                break;
                            }
                        }
                    }
                }
            }

            return first;
        }
    }
}