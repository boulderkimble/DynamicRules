using Xunit;
using RulesEngine.Dynamic;
using RulesEngine.Loader;


public class RulesEngineYamlTests
{

  private static RulesEngineLoader _loader = new();

  public class Person
  {
    public DateTime Birthdate { get; set; }
  }

  private static IDictionary<string, object> ToDictionary(object obj)
  {
    return new Dictionary<string, object>
      {
          { obj.GetType().Name, obj }
      };
  }


  [Theory]
  [InlineData("2000-01-01", true)] // Age >= 18
  [InlineData("2010-01-01", false)] // Age < 18
  public void IsAdultRule_Works(string birthdate, bool expected)
  {
    var yaml = @"
Parameters:
  - Name: Age
    Expression: 2025 - Person.Birthdate.Year
Rules:
  - Id: IsAdult
    Condition: Age >= 18
    Then: '""Age""'
";
    var rules = _loader.LoadRulesFromYaml(yaml);
    var processor = new DynamicRuleProcessor(rules);
    var person = new Person { Birthdate = DateTime.Parse(birthdate) };
    var result = processor.CoreEngine.Run(ToDictionary(person));
    Assert.Equal(expected, processor.CoreEngine.Artifacts.Select(p => p.Value).Contains("Age"));
  }

  [Theory]
  [InlineData("1950-01-01", true)] // Age >= 65
  public void NestedRule_Works(string birthdate, bool isSenior)
  {
    var yaml = @"
Parameters:
  - Name: Age
    Expression: 2025 - Person.Birthdate.Year
Rules:
  - Id: IsAdult
    Condition: Age >= 18
    Operator: AND
    Rules:
      - Id: IsSenior
        Condition: Age >= 65
        Then: '""Senior""'
    Then: '""Adult""'
";
    var rules = _loader.LoadRulesFromYaml(yaml);
    var processor = new DynamicRuleProcessor(rules);
    var person = new Person { Birthdate = DateTime.Parse(birthdate) };
    var result = processor.CoreEngine.Run(ToDictionary(person));
    Assert.Equal(isSenior, processor.CoreEngine.Artifacts.Select(a => a.Value).Contains("Senior") && processor.CoreEngine.Artifacts.Select(a => a.Value).Contains("Adult"));
  }



  [Theory]
  [InlineData("2006-01-01", true)] // Age 19
  [InlineData("2013-01-01", false)] // Age 12
  public void LocalParameterRule_Works(string birthdate, bool isTeen)
  {
    var yaml = @"
Parameters:
  - Name: Age
    Expression: 2025 - Person.Birthdate.Year
Rules:
  - Id: IsTeen
    Parameters:
      - Name: IsTeen
        Expression: Age >= 13 && Age <= 19
    Condition: IsTeen
    Then: '""IsTeen""'
";
    var rules = _loader.LoadRulesFromYaml(yaml);
    var processor = new DynamicRuleProcessor(rules);
    var person = new Person { Birthdate = DateTime.Parse(birthdate) };
    var result = processor.CoreEngine.Run(ToDictionary(person));
    Assert.Equal(isTeen, processor.CoreEngine.Artifacts.Select(a => a.Value).Contains("IsTeen"));
  }

  [Theory]
  [InlineData("1950-01-01", true, true)] // Age >= 18, IsAdult passes, IsSenior passes
  [InlineData("1970-01-01", true, false)] // Age >= 18, IsAdult passes, IsSenior fails
  [InlineData("2010-01-01", false, false)] // Age < 18, IsAdult fails, IsSenior not run
  public void RuleDependency_Works(string birthdate, bool isAdult, bool isSenior)
  {
    var yaml = @"
Parameters:
  - Name: Age
    Expression: 2025 - Person.Birthdate.Year
Rules:
  - Id: IsAdult
    Condition: Age >= 18
    Then: '""Adult""'
  - Id: IsSenior
    DependsOn: 
      - IsAdult
    Condition: Age >= 65
    Then: '""Senior""'
";
    var rules = _loader.LoadRulesFromYaml(yaml);
    var processor = new DynamicRuleProcessor(rules);
    var person = new Person { Birthdate = DateTime.Parse(birthdate) };
    var result = processor.CoreEngine.Run(ToDictionary(person));
    Assert.Equal(isAdult, processor.CoreEngine.Artifacts.Select(a => a.Value).Contains("Adult"));
    Assert.Equal(isSenior, processor.CoreEngine.Artifacts.Select(a => a.Value).Contains("Senior"));
  }

  // Anonymous type tests
  [Theory]
  [InlineData("2000-01-01", true)] // Age >= 18
  [InlineData("2010-01-01", false)] // Age < 18
  public void IsAdultRule_Anonymous_Works(string birthdate, bool expected)
  {
    var yaml = @"
Parameters:
  - Name: Age
    Expression: 2025 - Person.Birthdate.Year
Rules:
  - Id: IsAdult
    Condition: Age >= 18
    Then: '""Age""'
";
    var person = new { Birthdate = DateTime.Parse(birthdate) };
    var rules = _loader.LoadRulesFromYaml(yaml);
    var processor = new DynamicRuleProcessor(rules);
    var result = processor.CoreEngine.Run(new Dictionary<string, object> { { "Person", person } });
    foreach (var a in processor.CoreEngine.Artifacts)
      Console.WriteLine($"{a.Value.GetType().FullName}: {a.Value}");
    Assert.Equal(expected, processor.CoreEngine.Artifacts.Select(a => a.Value).Contains("Age"));
  }

  [Theory]
  [InlineData("1950-01-01", true)] // Age >= 65
  public void NestedRule_Anonymous_Works(string birthdate, bool isSenior)
  {
    var yaml = @"
Parameters:
  - Name: Age
    Expression: 2025 - Person.Birthdate.Year
Rules:
  - Id: IsAdult
    Parameters:
      - Name: AgeInMinutes
        Expression: Age * 365 * 24 * 60
    Condition: Age >= 18
    Operator: AND
    Rules:
      - Id: IsSenior
        Condition: Age >= 65 && AgeInMinutes > 0
        Then: '""Senior""'
    Then: '""Adult""'
";
    var person = new { Birthdate = DateTime.Parse(birthdate) };
    var rules = _loader.LoadRulesFromYaml(yaml);
    var processor = new DynamicRuleProcessor(rules);
    var result = processor.CoreEngine.Run(new Dictionary<string, object> { { "Person", person } });
    Assert.Equal(isSenior, processor.CoreEngine.Artifacts.Select(a => a.Value).Contains("Senior") && processor.CoreEngine.Artifacts.Select(a => a.Value).Contains("Adult"));
  }

  [Theory]
  [InlineData("2006-01-01", true)] // Age 19
  [InlineData("2013-01-01", false)] // Age 12
  public void LocalParameterRule_Anonymous_Works(string birthdate, bool isTeen)
  {
    var yaml = @"
Parameters:
  - Name: Age
    Expression: 2025 - Person.Birthdate.Year
Rules:
  - Id: IsTeen
    Parameters:
      - Name: IsTeen
        Expression: Age >= 13 && Age <= 19
    Condition: IsTeen
    Then: '""IsTeen""'
";
    var person = new { Birthdate = DateTime.Parse(birthdate) };
    var rules = _loader.LoadRulesFromYaml(yaml);
    var processor = new DynamicRuleProcessor(rules);
    var result = processor.CoreEngine.Run(new Dictionary<string, object> { { "Person", person } });
    Assert.Equal(isTeen, processor.CoreEngine.Artifacts.Select(a => a.Value).Contains("IsTeen"));
  }

  [Theory]
  [InlineData("1950-01-01", true, true)] // Age >= 18, IsAdult passes, IsSenior passes
  [InlineData("1970-01-01", true, false)] // Age >= 18, IsAdult passes, IsSenior fails
  [InlineData("2010-01-01", false, false)] // Age < 18, IsAdult fails, IsSenior not run
  public void RuleDependency_Anonymous_Works(string birthdate, bool isAdult, bool isSenior)
  {
    var yaml = @"
Parameters:
  - Name: Age
    Expression: 2025 - Person.Birthdate.Year
Rules:
  - Id: IsAdult
    Condition: Age >= 18
    Then: '""Adult""'
  - Id: IsSenior
    DependsOn: 
      - IsAdult
    Condition: Age >= 65
    Then: '""Senior""'
";
    var person = new { Birthdate = DateTime.Parse(birthdate) };
    var rules = _loader.LoadRulesFromYaml(yaml);
    var processor = new DynamicRuleProcessor(rules);
    var result = processor.CoreEngine.Run(new Dictionary<string, object> { { "Person", person } });
    Assert.Equal(isAdult, processor.CoreEngine.Artifacts.Select(a => a.Value).Contains("Adult"));
    Assert.Equal(isSenior, processor.CoreEngine.Artifacts.Select(a => a.Value).Contains("Senior"));
  }

  // Multiple input tests
  [Theory]
  [InlineData("2000-01-01", 100000, true)] // Age >= 18 and Salary >= 50000
  [InlineData("2010-01-01", 100000, false)] // Age < 18
  [InlineData("2000-01-01", 40000, false)] // Salary < 50000
  public void MultipleInputsRule_Works(string birthdate, int salary, bool expected)
  {
    var yaml = @"
Parameters:
  - Name: Age
    Expression: 2025 - Person.Birthdate.Year
  - Name: IsHighEarner
    Expression: Salary.Rate > 50000
Rules:
  - Id: IsAdultHighEarner
    Condition: Age >= 18 && IsHighEarner
    Then: '""Qualified""'
";
    var person = new { Birthdate = DateTime.Parse(birthdate) };
    var salaryObj = new { Rate = salary };
    var rules = _loader.LoadRulesFromYaml(yaml);
    var processor = new DynamicRuleProcessor(rules);
    var inputs = new Dictionary<string, object> { { "Person", person }, { "Salary", salaryObj } };
    var result = processor.CoreEngine.Run(inputs);
    Assert.Equal(expected, processor.CoreEngine.Artifacts.Select(a => a.Value).Contains("Qualified"));
  }

  [Theory]
  [InlineData("2000-01-01", 100000, true, true)] // Both rules pass
  [InlineData("2010-01-01", 100000, false, true)] // Only salary rule passes
  [InlineData("2000-01-01", 40000, true, false)] // Only age rule passes
  [InlineData("2010-01-01", 40000, false, false)] // Neither rule passes
  public void MultipleRules_MultipleInputs_Works(string birthdate, int salary, bool isAdult, bool isHighEarner)
  {
    var yaml = @"
Parameters:
  - Name: Age
    Expression: 2025 - Person.Birthdate.Year
  - Name: IsHighEarner
    Expression: Salary.Rate > 50000
Rules:
  - Id: IsAdult
    Condition: Age >= 18
    Then: '""Adult""'
  - Id: HighEarner
    Condition: IsHighEarner
    Then: '""HighEarner""'
";
    var person = new { Birthdate = DateTime.Parse(birthdate) };
    var salaryObj = new { Rate = salary };
    var rules = _loader.LoadRulesFromYaml(yaml);
    var processor = new DynamicRuleProcessor(rules);
    var inputs = new Dictionary<string, object> { { "Person", person }, { "Salary", salaryObj } };
    var result = processor.CoreEngine.Run(inputs);
    Assert.Equal(isAdult, processor.CoreEngine.Artifacts.Select(a => a.Value).Contains("Adult"));
    Assert.Equal(isHighEarner, processor.CoreEngine.Artifacts.Select(a => a.Value).Contains("HighEarner"));
  }


  [Theory]
  [InlineData("1950-01-01", true, true)] // Age >= 18, IsAdult passes, IsSenior passes
  [InlineData("1970-01-01", true, false)] // Age >= 18, IsAdult passes, IsSenior fails
  [InlineData("2010-01-01", false, false)] // Age < 18, IsAdult fails, IsSenior not run
  public async Task RuleDependency_Anonymous_Async_Works(string birthdate, bool isAdult, bool isSenior)
  {
    var yaml = @"
  Parameters:
    - Name: Age
      Expression: 2025 - Person.Birthdate.Year
  Rules:
    - Id: IsAdult
      Condition: Age >= 18
      Then: '""Adult""'
    - Id: IsSenior
      DependsOn: 
        - IsAdult
      Condition: Age >= 65
      Then: '""Senior""'
  ";
    var person = new { Birthdate = DateTime.Parse(birthdate) };
    var rules = _loader.LoadRulesFromYaml(yaml);
    var processor = new DynamicRuleProcessor(rules);
    await processor.CoreEngine.RunAsync(new Dictionary<string, object> { { "Person", person } });
    Assert.Equal(isAdult, processor.CoreEngine.Artifacts.Select(a => a.Value).Contains("Adult"));
    Assert.Equal(isSenior, processor.CoreEngine.Artifacts.Select(a => a.Value).Contains("Senior"));
  }
  
  /*
  [Fact]
  public void DynamicInput_ExpandoObject_Works()
  {
    var yaml = @"
  Parameters:
    - Name: Age
      Expression: 2025 - Person.Birthdate.Year
  Rules:
    - Id: IsAdult
      Condition: Age >= 18
      Then: '""Adult""'
  "; 
    dynamic person = new System.Dynamic.ExpandoObject();
    person.Birthdate = DateTime.Parse("2000-01-01");
    var rules = _loader.LoadRulesFromYaml(yaml);
    var processor = new DynamicRuleProcessor(rules);
    var inputs = new Dictionary<string, object> { { "Person", person } };
    var result = processor.CoreEngine.Run(inputs);
    Assert.Contains("Adult", processor.CoreEngine.Artifacts.Select(a => a.Value));
  }
  */
}
