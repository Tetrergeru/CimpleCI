using System;
using System.Collections.Generic;
using System.Linq;
using Frontend.AST;
using Frontend.Lexer;

namespace Frontend.Parser.Ll1Parser
{
    internal interface IConsumer
    {
        bool CanConsume(int peek);

        List<int> Start();

        string ToString(SymbolDictionary sd, int offset);
    }

    internal class FinalConsumer : IConsumer
    {
        public int Rule;

        public FinalConsumer(int rule)
        {
            Rule = rule;
        }

        public bool CanConsume(int peek)
            => true;

        public List<int> Start()
            => new List<int>();

        public string ToString(SymbolDictionary sd, int offset)
            => new string(' ', offset) + $"Final({Rule})";
    }

    internal class SymbolConsumer : IConsumer
    {
        private readonly HashSet<int> _prefixes;
        public readonly Func<IASTNode> Action;
        public readonly IConsumer Consumer;

        public SymbolConsumer(HashSet<int> prefixes, Func<IASTNode> action, IConsumer consumer)
        {
            _prefixes = prefixes;
            Action = action;
            Consumer = consumer;
        }

        public bool CanConsume(int peek)
            => _prefixes.Contains(peek);

        public List<int> Start()
            => _prefixes.ToList();

        public string ToString(SymbolDictionary sd, int offset)
            => new string(' ', offset) +
               $"Symbol({string.Join(", ", _prefixes.Select(i => sd[i].name))}) ->\n{Consumer.ToString(sd, offset + 3)}";
    }

    class SwitchConsumer : IConsumer
    {
        private readonly List<IConsumer> _switch;

        public IConsumer Go(int token)
            => _switch.FirstOrDefault(c => c.CanConsume(token));


        public SwitchConsumer(List<IConsumer> @switch)
        {
            _switch = @switch;
        }

        public bool CanConsume(int peek)
            => _switch.Any(p => p.CanConsume(peek));

        public List<int> Start()
            => _switch.SelectMany(x => x.Start()).ToList();

        public string ToString(SymbolDictionary sd, int offset)
            => new string(' ', offset) +
               $"Switch:\n{string.Join("\n", _switch.Select(c => c.ToString(sd, offset + 3)))}\n";
    }
}