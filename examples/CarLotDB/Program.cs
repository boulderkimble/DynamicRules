
using RulesEngine.Dynamic;
using RulesEngine.Loader;
using CarLotDB;
using Microsoft.EntityFrameworkCore;

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



