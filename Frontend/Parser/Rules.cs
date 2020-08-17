using System.Collections.Generic;

namespace Frontend.Parser
{
    public class Rules
    {
        public List<Rule> RuleList;

        private Dictionary<int, List<int>> RuleGroups = new Dictionary<int, List<int>>();
        
        public Rules(List<Rule> ruleList)
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

        public Rule this[int index] => RuleList[index];

        public IEnumerable<int> RuleGroup(int nonTerminal) 
            => RuleGroups[nonTerminal];
    }
}