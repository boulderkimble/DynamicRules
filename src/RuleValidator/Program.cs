using System.Linq.Dynamic.Core;
using YamlDotNet.Serialization;
using System.Reflection;
using System.Linq.Expressions;
using System.Linq.Dynamic.Core.CustomTypeProviders;
using System.Dynamic;
using System.Text.Json;
using RulesEngine.Core;
using RulesEngine.Dynamic;

class Program
{
    static int Main(string[] args)
    {
        var pairs = new Dictionary<string, Type>();
        // Usage: YmlValidator <rules.yaml> [<AssemblyPath>]
        if (args.Length == 0 || !File.Exists(args[0]))
        {
            Console.Error.WriteLine("Usage: YmlValidator <rules.yaml> [<FullyQualifiedTypeName> <AssemblyPath>]");
            return 1;
        }
                
        if (args.Length >= 2)
        {
            var assemblyPaths = args[1].Split(',').Select(p => p.Trim()).Where(p => !string.IsNullOrEmpty(p)).ToArray();
            
            foreach (var assemblyPath in assemblyPaths)
            {
                if (!File.Exists(assemblyPath))
                {
                    Console.Error.WriteLine($"Warning: Assembly '{assemblyPath}' not found. Skipping.");
                    continue;
                }
                try
                {
                    var asm = Assembly.LoadFrom(assemblyPath);
                    
                    var inputMapType = asm.GetTypes()
                        .FirstOrDefault(t =>
                            t.IsClass &&
                            t.IsAbstract && t.IsSealed && // static class
                            t.GetCustomAttributes(typeof(VSExtensionConfigAttribute), false).Any() && // has attribute
                            t.GetMethod("ReturnInputMapForExtension", BindingFlags.Public | BindingFlags.Static) != null
                        );

                    if (inputMapType != null)
                    {
                        var method = inputMapType.GetMethod("ReturnInputMapForExtension", BindingFlags.Public | BindingFlags.Static);
                        var result = method?.Invoke(null, null) as IEnumerable<(string?, Type)>;
                        if (result != null)
                        {
                            foreach (var (name, type) in result)
                            {
                                var inputName = !string.IsNullOrWhiteSpace(name)
                                    ? name
                                    : type?.Name; // Use short type name if name is null or empty

                                if (!string.IsNullOrWhiteSpace(inputName) && type != null)
                                    pairs[inputName] = type;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Warning: Could not load assembly '{assemblyPath}': {ex.Message}");
                }
            }
        }

        var filePath = args[0];
        string fileContent = File.ReadAllText(filePath);

        DynamicRules ruleSet;
        try
        {
            if (filePath.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            {
                ruleSet = JsonSerializer.Deserialize<DynamicRules>(fileContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                })!;
                if (ruleSet == null)
                {
                    Console.Error.WriteLine("Error: Failed to deserialize JSON file into DynamicRules.");
                    return 2;
                }
            }
            else if (filePath.EndsWith(".yaml", StringComparison.OrdinalIgnoreCase) || filePath.EndsWith(".yml", StringComparison.OrdinalIgnoreCase))
            {
                var deserializer = new DeserializerBuilder().Build();
                ruleSet = deserializer.Deserialize<DynamicRules>(fileContent);
            }
            else
            {
                Console.Error.WriteLine("Unsupported file format. Please use .yaml, .yml, or .json.");
                return 1;
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Parse error: {ex.Message}");
            return 2;
        }

        int errors = 0;
        var config = new ParsingConfig
        {
            AllowEqualsAndToStringMethodsOnObject = true,
            LoadAdditionalAssembliesFromCurrentDomainBaseDirectory = false,
            AllowNewToEvaluateAnyType = true
        };
        config.UseDefaultDynamicLinqCustomTypeProvider([typeof(System.Linq.Enumerable), typeof(System.Collections.Generic.Dictionary<,>)], true);

        List<ParameterExpression> globalExpressions = pairs
            .Select(kvp => Expression.Parameter(kvp.Value, kvp.Key))
            .ToList();

        globalExpressions.Add(Expression.Parameter(typeof(IEnumerable<string>), "Farts"));


        // Recursive rule validation
        void ValidateRule(DynamicRule rule, List<DynamicRuleParameter> globalParams)
        {
            var paramExprs = globalExpressions.ToList();

            foreach (var param in globalParams)
            {
                if (!string.IsNullOrWhiteSpace(param.Expression))
                {
                    try
                    {

                        var lambda = DynamicExpressionParser.ParseLambda(config,
                            [.. paramExprs],
                            null,
                            param.Expression);
                        paramExprs.Add(Expression.Parameter(lambda.ReturnType, param.Name));
                        

                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Global Parameter '{param.Name}' error: {ex.Message}");
                        errors++;
                    }
                }
            }
            
            // Param validation
            foreach (var param in rule.Parameters)
            {
                if (!string.IsNullOrWhiteSpace(param.Expression))
                {
                    try
                    {
                        var lambda = DynamicExpressionParser.ParseLambda(config,
                            [.. paramExprs],
                            null,
                            param.Expression);
                        paramExprs.Add(Expression.Parameter(lambda.ReturnType, param.Name));
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Rule '{rule.Id}' Parameter '{param.Name}' error: {ex.Message}");
                        errors++;
                    }
                }
            }

            // Validate Condition
            if (!string.IsNullOrWhiteSpace(rule.Condition))
            {
                try
                {
                    var lambda = DynamicExpressionParser.ParseLambda(config,
                        [.. paramExprs],
                        typeof(bool),
                        rule.Condition
                    );
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Rule '{rule.Id}' Condition error: {ex.Message}");
                    errors++;
                }
            }
            // Validate Then
            if (!string.IsNullOrWhiteSpace(rule.Then))
            {
                try
                {
                    var lambda = DynamicExpressionParser.ParseLambda(config,
                        [.. paramExprs],
                        null,
                        rule.Then);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Rule '{rule.Id}' Then error: {ex.Message}");
                    errors++;
                }
            }
            // Local scope params override global
            var effectiveGlobalParams = globalParams
                .Where(p => !rule.Parameters.Any(rp => rp.Name == p.Name))
                .ToList();

            var scopedParameters = effectiveGlobalParams.Concat(rule.Parameters).ToList();
            if (rule.Rules != null)
            {
                foreach (var child in rule.Rules)
                {
                    ValidateRule(child, scopedParameters);
                }
            }
        }

        foreach (var rule in ruleSet.Rules)
        {
            ValidateRule(rule, ruleSet.Parameters);
        }

        if (errors == 0)
        {
            Console.WriteLine("All expressions are valid.");
            return 0;
        }
        else
        {
            Console.WriteLine($"{errors} error(s) found.");
            return 3;
        }
    }


}
