using System.Collections.Generic;
using System.Linq;

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
    }
}