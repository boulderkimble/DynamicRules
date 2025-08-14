using RulesEngine.Core;
using RulesEngine.Dynamic;
using YamlDotNet.Serialization;
using System.Text.Json;

namespace RulesEngine.Loader
{
    public class RulesEngineLoader : RulesEngineLoaderBase
    {

        public override DynamicRules LoadRulesFromYaml(string yaml)
        {
            var deserializer = new DeserializerBuilder().Build();
            var yamlRules = deserializer.Deserialize<DynamicRules>(yaml);
            return yamlRules;
        }

        public override DynamicRules LoadRulesFromYaml(FileInfo yamlFile)
        {
            if (!yamlFile.Exists)
                throw new FileNotFoundException("YAML file not found.", yamlFile.FullName);

            var yamlContent = File.ReadAllText(yamlFile.FullName);
            return LoadRulesFromYaml(yamlContent);
        }

        public override DynamicRules LoadRulesFromJson(string json)
        {
            return JsonSerializer.Deserialize<DynamicRules>(json);
        }

        public override DynamicRules LoadRulesFromJson(FileInfo jsonFile)
        {
            if (!jsonFile.Exists)
                throw new FileNotFoundException("JSON file not found.", jsonFile.FullName);

            var jsonContent = File.ReadAllText(jsonFile.FullName);
            return LoadRulesFromJson(jsonContent);
        }
        
    }
    
}