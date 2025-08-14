
using System.Text.Json;
using RulesEngine.Dynamic;
using RulesEngine.Loader;
using CarLot;

var jsonData = await File.ReadAllTextAsync("testdata.json");
var carData = JsonSerializer.Deserialize<List<Auto>>(jsonData);
var rulesLoader = new RulesEngineLoader();
var rules = rulesLoader.LoadRulesFromYaml(new FileInfo("rules_sample1.yaml"));
var processor = new DynamicRuleProcessor(rules);


if (carData == null)
    throw new InvalidOperationException("Failed to deserialize car data");

processor.CoreEngine.ArtifactAdded += (artifact) =>
{
    Console.WriteLine($"Rule succeeded and artifact added for Rule: {artifact.Id}");

    if (artifact.Value != null)
    {
        Console.WriteLine($"Artifact Object: {artifact.Value}\n");
    }
};

foreach (var car in carData)
{
    processor.CoreEngine.Run(car);
}


    

