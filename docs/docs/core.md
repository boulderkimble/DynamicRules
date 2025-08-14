# RulesEngine.Core

Aside from externally defined rules in JSON/YAML, this project exposes a C# rules engine that powers the dynamic rule processiing. The dynamic rule processor builds a set of _RulesEngine.Core.Rule_ rules internally, but the core interface can be used on its own for rules defined in code.

If you're looking for a .NET rules engine focused purely on C#, [nRules](https://nrules.net) might be the better choice. On the other hand it has a simple fluent interface and might be suitable depending on your requirements.

Not much documentation to offer at this time. Here is an example:

```
using RulesEngine.Core;
.
.
var isOldCarRule = new Rule("IsOldCar")
    .When((rule, parameters) =>
    {
        var auto = parameters["Auto"] as Auto;
        return auto != null && auto.Age > 10;
    })
    .Then((rule, parameters, id) => $"Auto {((Auto)parameters["Auto"]).VIN} is old.");

var hasRecentCollisionRule = new Rule("HasRecentCollision")
    .When((rule, parameters) =>
    {
        var auto = parameters["Auto"] as Auto;
        if (auto == null) return false;
        var oneYearAgo = DateTime.Now.AddYears(-1);
        return auto.CollisionDates.Exists(d => d > oneYearAgo);
    })
    .Then((rule, parameters, id) => $"Auto {((Auto)parameters["Auto"]).VIN} has a recent collision.");

var highRiskRule = new Rule("HighRisk")
    .When((rule, parameters) =>
        parameters.TryGetValue("IsOldCar", out var isOld) && (bool)isOld &&
        parameters.TryGetValue("HasRecentCollision", out var hasRecent) && (bool)hasRecent)
    .DependsOn("IsOldCar", "HasRecentCollision")
    .Then((rule, parameters, id) => $"Auto {((Auto)parameters["Auto"]).VIN} is high risk!");

// Add rules to the engine
var engine = new RulesEngineCore(new[] { isOldCarRule, hasRecentCollisionRule, highRiskRule });

// Example input
var auto = new Auto
{
    VIN = "123ABC",
    Age = 12,
    CollisionDates = new List<DateTime> { DateTime.Now.AddMonths(-6) }
};

// Run the engine
var errors = engine.Run(("Auto", auto));

// Output results
if (errors.Count == 0)
{
    Console.WriteLine("All rules passed!");
    foreach (var artifact in engine.Artifacts)
        Console.WriteLine($"Artifact: {artifact.Value}");
}
else
{
    Console.WriteLine("Errors:");
    foreach (var error in errors)
        Console.WriteLine(error);
}
```
