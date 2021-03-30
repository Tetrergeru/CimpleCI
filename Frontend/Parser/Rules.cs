using System;
using System.Collections.Generic;
using System.Linq;
using Frontend.Lexer;

namespace Frontend.Parser
{
    public class Rules<T>
    {
        public List<Rule<T>> RuleList;

        private Dictionary<int, List<int>> RuleGroups = new Dictionary<int, List<int>>();
        
        public Rules(List<Rule<T>> ruleList)
        {
            RuleList = ruleList;
            for (var i = 0; i < RuleList.Count; i++)
            {
                var rule = ruleList[i];
                if (!RuleGroups.ContainsKey(rule.NonTerminal))
                    RuleGroups[rule.NonTerminal] = new List<int>();
                RuleGroups[rule.NonTerminal].Add(i);
            }
        }

        public Rule<T> this[int index] => RuleList[index];

        public List<int> RuleGroup(int nonTerminal) 
            => RuleGroups[nonTerminal];
        
        public List<Rule<T>> FullRuleGroup(int nonTerminal) 
            => RuleGroups[nonTerminal].Select(id => RuleList[id]).ToList();

        public void Print(SymbolDictionary sd)
        {
            foreach (var rule in RuleList)
            {
                Console.Write(sd[rule.NonTerminal].name);
                Console.Write(" -> ");
                Console.Write(string.Join(" ", rule.Sequence.Select(s => sd[s].name)));
                Console.WriteLine($" {{ {rule.Callback} }}");
            }
        }
    }
}