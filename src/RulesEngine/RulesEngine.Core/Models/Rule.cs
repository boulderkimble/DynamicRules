namespace RulesEngine.Core
{
    public class Rule
    {
        public string Id { get; private set; }
        public List<RuleDependency> DependentRules { get; } = new();
        public List<Rule> Rules { get; } = new();
        public string Operator { get; set; } = "AND"; // "AND" or "OR"
        public Dictionary<string, object> Context { get; } = new();

        // rule, parameters, rule
        private Func<Rule, IDictionary<string, object>, bool> _condition;

        // rule, parameters, artifacts, rule id
        private Func<Rule, IDictionary<string, object>, string?, object?> _onRuleSuccess = (_, _, _) => { return null; };

        public Rule(string id, Func<Rule, IDictionary<string, object>, bool> condition)
        {
            Id = id;
            _condition = condition;
        }

        public static Rule When(string id, Func<Rule, IDictionary<string, object>, bool> condition)
        {
            return new Rule(id, condition);
        }

        public Rule DependsOn(params (string ruleId, bool expectedResult)[] dependencies)
        {
            DependentRules.AddRange(dependencies.Select(d => new RuleDependency(d.ruleId, d.expectedResult)));
            return this;
        }

        public Rule Then(Func<Rule, IDictionary<string, object>, string?, object?> onRuleSuccess)
        {
            _onRuleSuccess = onRuleSuccess;
            return this;
        }


        public bool Run(IDictionary<string, object> parameters)
        {
            if (parameters == null || parameters.Count == 0)
                throw new ArgumentException("Parameters cannot be null or empty.");

            bool mainResult = _condition(this, parameters);

            if (Rules.Count == 0)
                return mainResult;

            var childResults = Rules.Select(child => child.Run(parameters)).ToList();
            bool childrenResult = Operator.Equals("OR", StringComparison.OrdinalIgnoreCase)
                ? childResults.Any(r => r)
                : childResults.All(r => r);

            return mainResult && childrenResult;
        }

        public object? RunSuccess(IDictionary<string, object> parameters, string? id = null)
        {
            return _onRuleSuccess.Invoke(this, parameters, id);
        }
    }
}