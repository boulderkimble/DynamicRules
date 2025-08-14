namespace RulesEngine.Dynamic
{
    public class DynamicRules
    {
        public virtual List<DynamicRuleParameter> Parameters { get; set; } = [];
        public virtual List<DynamicRule> Rules { get; set; } = [];
    }
}