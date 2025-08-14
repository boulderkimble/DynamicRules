namespace RulesEngine.Core
{
    public record RuleDependency(string RuleId, bool ExpectedResult = true);
}