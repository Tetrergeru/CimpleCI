using System;
using System.Collections.Generic;
using Frontend.AST;

namespace Frontend.Parser
{
    public struct Rule<T>
    {
        public readonly int NonTerminal;
        
        public readonly List<int> Sequence;

        public readonly RuleCallback Callback;
        
        public Rule(int nonTerminal, List<int> sequence, RuleCallback callback)
        {
            Sequence = sequence;
            Callback = callback;
            NonTerminal = nonTerminal;
        }
    }
}