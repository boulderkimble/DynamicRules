namespace RulesEngine.Dynamic
{
    public class DynamicRule
    {
        public virtual required string Id { get; set; }
        public virtual List<DynamicRuleParameter> Parameters { get; set; } = [];
        public virtual required string Condition { get; set; }
        public virtual IEnumerable<string> DependsOn { get; set; } = [];
        public virtual string? Then { get; set; }
        public virtual string Operator { get; set; } = "AND";
        public virtual List<DynamicRule> Rules { get; set; } = [];
    }
}