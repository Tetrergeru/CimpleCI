using System;
using System.Collections.Generic;
using System.Linq;
using Frontend.AST;
using Frontend.Lexer;

namespace Frontend.Parser.Ll1Parser
{
    internal interface IConsumer<T>
    {
        bool CanConsume(int peek);

        List<int> Start();

        string ToString(SymbolDictionary sd, int offset);
    }

    internal class FinalConsumer<T> : IConsumer<T> 
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

    internal class SymbolConsumer<T>  : IConsumer<T> 
    {
        private readonly HashSet<int> _prefixes;
        public readonly Func<T> Action;
        public readonly IConsumer<T>  Consumer;

        public SymbolConsumer(HashSet<int> prefixes, Func<T> action, IConsumer<T>  consumer)
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

    class SwitchConsumer<T>  : IConsumer<T> 
    {
        private readonly List<IConsumer<T> > _switch;

        public IConsumer<T>  Go(int token)
            => _switch.FirstOrDefault(c => c.CanConsume(token));


        public SwitchConsumer(List<IConsumer<T> > @switch)
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