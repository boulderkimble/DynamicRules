# Example

See the examples folder for more examples.

```

# Global parameters
Parameters:
    # So that this demo will always run the same results from our testdata.json file, we set a fixed date.
    # In a real-world scenario, you would use DateTime.Now or similar. 
  - Name: DemoDate
    Expression: DateTime.Parse("2025-08-01")
  - Name: IsOldCar
    Expression: Auto.Age > 10
  - Name: HasRecentCollision
    Expression: Collisions.Any(c => c.VIN == Auto.VIN && c.Date > DemoDate.AddYears(-1))
Rules:
  - Id: rule_high_risk
    Condition: IsOldCar && HasRecentCollision
    Then: '"VIN: " + Auto.VIN + " High Risk"'

--------------------------------------------
[DynamicLinqType]
public class CarLotContext : DbContext
{
    public DbSet<Auto> Autos { get; set; }
    public DbSet<Collision> Collisions { get; set; }

    public CarLotContext(DbContextOptions<CarLotContext> options)
        : base(options) { }
}


var options = new DbContextOptionsBuilder<CarLotContext>()
    .UseSqlite("Data Source=TestDb.sqlite")
    .Options;

using var context = new CarLotContext(options);
context.Database.EnsureCreated();

if (!context.Autos.Any())
{
    var auto1 = new Auto { VIN = "1A4AABBC5501999", Make = "Toyota", Age = 12 };
    var auto2 = new Auto { VIN = "2B3CA4CD5GH123456", Make = "Honda", Age = 5 };

    context.Autos.AddRange(auto1, auto2);

    context.Collisions.AddRange(
        new Collision { VIN = auto1.VIN, Date = new DateTime(2025, 9, 1) },
        new Collision { VIN = auto1.VIN, Date = new DateTime(2024, 8, 15) },
        new Collision { VIN = auto2.VIN, Date = new DateTime(2025, 1, 10) },
        new Collision { VIN = auto2.VIN, Date = new DateTime(2022, 1, 10) }
    );
}

context.SaveChanges();

var rulesLoader = new RulesEngineLoader();
var rules = rulesLoader.LoadRulesFromYaml(new FileInfo("rules_sample1.yaml"));
var processor = new DynamicRuleProcessor(rules);
processor.CoreEngine.ArtifactAdded += (artifact) =>
{
    Console.WriteLine($"Rule succeeded and artifact added for Rule: {artifact.Id}");

    if (artifact.Value != null)
    {
        Console.WriteLine($"Artifact Object: {artifact.Value}\n");
    }
};

foreach (var auto in context.Autos)
{
    processor.CoreEngine.Run(
        ("Auto", auto),
        ("Collisions", context.Collisions)
    );
}
```