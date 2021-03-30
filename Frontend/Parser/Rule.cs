using System;
using System.Collections.Generic;
using Frontend.AST;

namespace Frontend.Parser
{
    public struct Rule<T>
    {
        public readonly int NonTerminal;
        
        public readonly IReadOnlyList<int> Sequence;

        public readonly Callable<T> Callback;
        
        public Rule(int nonTerminal, IReadOnlyList<int> sequence, Callable<T> callback)
        {
            Sequence = sequence;
            Callback = callback;
            NonTerminal = nonTerminal;
        }
    }
}