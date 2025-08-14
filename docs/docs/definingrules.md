# Defining Rules

```
# Global parameters
Parameters:
  - Name: DemoDate
    Expression: DateTime.Now
  - Name: IsOldCar
    Expression: Auto.Age > 10
  - Name: HasRecentCollision
    Expression: Collisions.Any(c => c.VIN == Auto.VIN && c.Date > DemoDate.AddYears(-1))
Rules:
  - Id: rule_low_risk
    # Note expressions with boolean ! must be included in quotes because it's a reserved tag in YAML
    Condition: "!IsOldCar && !HasRecentCollision"
  - Id: rule_high_risk
    Condition: IsOldCar && HasRecentCollision
    Then: '"VIN: " + Auto.VIN + " High Risk"'
  - Id: rule_requires_high_risk
    DependsOn: [rule_high_risk]
    Condition: true
    Then: '"This rule only runs if rule_high_risk succeeds."'
```

## Rules Anatomy 

Dynamic Rules are defined by the object definitions in RulesEngine.Dynamic/Models. A set of YAML or JSON rules is simply a serialization of the _DynamicRules_ C# type:

```
public class DynamicRules
{
    public virtual List<DynamicRuleParameter> Parameters { get; set; } = [];
    public virtual List<DynamicRule> Rules { get; set; } = [];
}
```

Let's review each feature with YAML.

### Parameters -> (Name, Expression)

Global parameters are in scope by all rules and sub-rules. These must be at the top of your rule definitions. Name identifies the parameter. Expression is the C# that the parameter evaulates to. Use the name to refer to the parameter in other expressions. 
```
Parameters:
  - Name: IsOldCar
    Expression: Auto.Age > 10
  - Name: HasRecentCollision
    Expression: Auto.CollisionDates.Any(d => d > DemoDate.AddYears(-1))
```

### Rules -> (Id, Condition, Then, Parameters, DependsOn, Operator, Rules)

##### Id 

  Unique name of rule

##### Condition

  C# expression that evaluates to a boolean

##### Then

  C# expression that evaluates to an object added to an artifact if Condition is satisfied. Note the rules engine always generates an artifact for a succussful rule and artifacts can be accessed from code. The _Then_ expression only adds an object to the artifact. See [Using Rules](using.md) for details. 

```
Rules:
  - Id: rule_high_risk
    Condition: IsOldCar && HasRecentCollision
    Then: '"High Risk: VIN " + Auto.VIN'
  - Id: rule_low_risk
    Condition: "!IsOldCar && !HasRecentCollision"
    Then: "RiskUtils.FlagLowRisk(Auto.Make, Auto.Age)"
```

#### Parameters -> (Name, Expression)
Rule scoped parameters. Name should be unique for the given rule. If it matches a global parameter, it overrides the global value. 
```
- Id: rule_critical_risk
    Parameters:
      - Name: RecentCollisionCount
        Expression: Auto.CollisionDates.Count(d => d > DemoDate.AddYears(-1))
    Condition: "!IsOldCar && HasRecentCollision"
    Then: '"Critical Review Needed: VIN:" + Auto.VIN + " Make:" + Auto.Make + " - Recent Collisions: " + RecentCollisionCount'
```

#### DependsOn
Value is an array of rule IDs that must all evaluate to true before this rule is evaluated. Rule IDs can reference other rules or sub-rules (including those from the same or different rule groups). The rules engine will throw an exception if a circular dependency is detected.
```
- Id: rule_high_risk_toyota
    DependsOn: [rule_high_risk]
    Condition: Auto.Make == "Toyota"
    Then: '"This rule only runs if Toyota and rule_high_risk succeeds."'
```

#### Rules
Subrules are nested rules within a parent rule. By default, all subrules must evaluate to true for the parent rule to succeed. 
```
Rules:
  - Id: main_rule
    Rules: 
      - Id: sub_rule_1
```
You can control how subrules are combined by specifying the optional Operator parameter, which accepts 'AND' (default) or 'OR':
```
Rules:
  - Id: main_rule
    Operator: OR
    Rules: 
      - Id: sub_rule_1
      - Id: sub_rule_2
```

