# Using the Library

## Prerequisites

### Dynamic Rules & Types

The engine must know the names and types of your inputs at runtime. The names must match what you use in your _Then_ and Parameter expressions. These are passed in the context of the Run() call to the rules engine:
```
    processor.CoreEngine.Run(
        ("Auto", auto),
        ("Collisions", context.Collisions)
    );
```

There are variants of Run that take just an enumeration of objects - no names. **Only use these if all of your inputs are user defined types and you use the type name(s) (e.g. "Auto") in your rule expressions**.

Aside from inputs, the rules engine must also know about user defined types used in expressions. These are often utility types with static methods. To define these you must use the ```DynamicLinqType``` attribute (defined in the [Dynamic LINQ library](https://github.com/zzzprojects/System.Linq.Dynamic.Core) which this project depends on).

```
    [DynamicLinqType]
    public static class RiskUtils
    {
        public static string FlagHighRisk(string make, int age, List<DateTime> collisionDates)
        {
            return $"High risk: {make}, Age: {age}, Collisions: {collisionDates.Count}";
        }

        public static string FlagLowRisk(string make, int age)
        {
            return $"Low risk: {make}, Age: {age}";
        }

        public static string FlagReview(string make, List<DateTime> collisionDates)
        {
            return $"Review needed: {make}, Recent collisions: {collisionDates.Count}";
        }

        public static string FlagCriticalReview(string make, int recentCollisionCount)
        {
            return $"Critical review: {make}, Recent collisions: {recentCollisionCount}";
        }
    }
```

```
    Parameters:
      - Name: HasRecentCollision
        Expression: Collisions.Any(c => c.VIN == Auto.VIN && c.Date > DemoDate.AddYears(-1))
    Rules:
       - Id: rule_low_risk
       # boolean ! must be included in quotes because it's a reserved tag in YAML
         Condition: "!IsOldCar && !HasRecentCollision"
       # Call a static method that returns a string.
         Then: "RiskUtils.FlagLowRisk(Auto.Make, Auto.Age)"
```

Finally if you want to use the VS Code extension to edit your rules, you will need to mark input types using an attribute. See [VS Code Extension](extension.md) for details.


## Running

- Load your rules from YAML or JSON (files or string)

```
    var rulesLoader = new RulesEngineLoader();
    var rules = rulesLoader.LoadRulesFromYaml(new FileInfo("rules_sample1.yaml"));
```

- Create a dynamic rules processor instance.

```
    var processor = new DynamicRuleProcessor(rules);
```

- Run the rules against your input data. The _CoreEngine_ member of the processor is the core rules engine instance. The processor populates the core engine with dynamically compiled delegates from the rule expressions. In the example below, we're using an entity framework context as the data source, but it could be objects from memory or other sources.
```
    foreach (var auto in context.Autos)
    {
        processor.CoreEngine.Run(
            ("Auto", auto),
            ("Collisions", context.Collisions)
        );
    }
```

- Consume/Process artifacts.

    Artifacts can be received by an event handler:

```
    processor.CoreEngine.ArtifactAdded += (artifact) =>
    {
        Console.WriteLine($"Rule succeeded and artifact added for Rule: {artifact.Id}");

        if (artifact.Value != null)
        {
            Console.WriteLine($"Artifact Object: {artifact.Value}\n");
        }
    };
```