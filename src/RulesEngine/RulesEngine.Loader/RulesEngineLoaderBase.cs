using System.Configuration;
using RulesEngine.Dynamic;

namespace RulesEngine.Loader
{
    public abstract class RulesEngineLoaderBase
    {
        public virtual DynamicRules LoadRulesFromYaml(string yaml)
            => throw new NotImplementedException();

        public virtual DynamicRules LoadRulesFromYaml(FileInfo yamlFile)
            => throw new NotImplementedException();

        public virtual DynamicRules LoadRulesFromJson(string json)
            => throw new NotImplementedException();

        public virtual DynamicRules LoadRulesFromJson(FileInfo jsonFile)
            => throw new NotImplementedException();

    }
}