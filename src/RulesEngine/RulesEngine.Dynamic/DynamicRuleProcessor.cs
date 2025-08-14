using RulesEngine.Core;
using System.Linq.Dynamic.Core;
using System.Linq.Dynamic.Core.CustomTypeProviders;
using System.Linq.Expressions;
using System.Runtime.Caching;

namespace RulesEngine.Dynamic
{
    
    
    [DynamicLinqType]
    public static class ObjectUtils
    {
        public static IDictionary<string, object> Create(params object[] properties)
        {
            var dictionary = new Dictionary<string, object>();
            for (int i = 0; i < properties.Length; i += 2)
            {
                var key = properties[i] as string;
                var value = properties[i + 1];
                dictionary[key] = value;
            }
            return dictionary;
        }
    }

    public class DynamicRuleProcessor
    {
        private class CachedDynamic
        {
            public List<ParameterExpression> ParamExprs;
            public List<Delegate> ParamEvaluators;
            public List<object>? InputValues;
            public Delegate? CompiledCond;
            public Delegate? CompiledThen;
        }

        [DynamicLinqType]
        public static class DateUtils
        {
            public static int Age(DateTime birthdate)
            {
                var today = DateTime.Today;
                int age = today.Year - birthdate.Year;
                if (birthdate > today.AddYears(-age)) age--;
                return age;
            }
        }

        /// <summary>
        /// Initializes a new instance of <see cref="DynamicRuleProcessor"/> with the provided dynamic rules.
        /// </summary>
        /// <param name="drules">The dynamic rules to process.</param>
        public DynamicRuleProcessor(DynamicRules drules)
        {
            var config = new ParsingConfig { AllowEqualsAndToStringMethodsOnObject = true };
            IList<Type> customTypes = [typeof(Enumerable), typeof(Dictionary<,>)];
            config.UseDefaultDynamicLinqCustomTypeProvider(customTypes, true);
            CoreEngine = new RulesEngineCore([.. drules.Rules.Select(drule => BuildRuleFromDynamic(drule, config, [.. drules.Parameters]))]);
        }

        /// <summary>
        /// Gets the underlying <see cref="RulesEngineCore"/> instance used for rule execution.
        /// </summary>
        public RulesEngineCore CoreEngine { get; }


        /// <summary>
        /// Prepares and caches dynamic parameter expressions and evaluators for a rule.
        /// </summary>
        /// <param name="config">The parsing configuration.</param>
        /// <param name="globalParams">Global parameters for the rule set.</param>
        /// <param name="ruleParams">Local parameters for the rule.</param>
        /// <param name="rule">The rule being processed.</param>
        /// <param name="ruleInputs">Input values for the rule.</param>
        /// <param name="inputKeys">Output list of input keys used for parameter expressions.</param>
        /// <returns>A cached dynamic context for the rule.</returns>
        private static CachedDynamic PrepareDynamic(
            ParsingConfig config,
            List<DynamicRuleParameter> globalParams,
            List<DynamicRuleParameter> ruleParams,
            Rule rule,
            IDictionary<string, object> ruleInputs,
            out List<string> inputKeys)
        {

            inputKeys = [.. ruleInputs.Select(kvp => kvp.Key).OrderBy(k => k)];

            if (rule.Context.GetValueOrDefault("_cachedDynamic") is not CachedDynamic cachedDynamic)
            {
                var pExprs = inputKeys.Select(k => Expression.Parameter(ruleInputs[k].GetType(), k)).ToList();

                var pEvaluators = new List<Delegate>();
                foreach (var p in GetScopedParameters(globalParams, ruleParams))
                {
                    var lambda = DynamicExpressionParser.ParseLambda(config, pExprs.ToArray(), null, p.Expression);
                    var compiled = lambda.Compile();
                    pEvaluators.Add(compiled);
                    pExprs.Add(Expression.Parameter(lambda.ReturnType, p.Name));
                }

                cachedDynamic = new CachedDynamic
                {
                    ParamExprs = pExprs,
                    ParamEvaluators = pEvaluators,
                    InputValues = null,
                    CompiledCond = null,
                    CompiledThen = null
                };

                rule.Context["_cachedDynamic"] = cachedDynamic;
            }

            return cachedDynamic;
        }

        /// <summary>
        /// Gets the effective scoped parameters for a rule, combining global and local parameters.
        /// </summary>
        private static List<DynamicRuleParameter> GetScopedParameters(
            List<DynamicRuleParameter>? globalParams,
            List<DynamicRuleParameter>? localParams)
        {
            var locals = localParams ?? new List<DynamicRuleParameter>();
            var effectiveGlobalParams = (globalParams ?? new List<DynamicRuleParameter>())
                .Where(gp => !locals.Any(rp => rp.Name == gp.Name))
                .ToList();

            return effectiveGlobalParams.Concat(locals).ToList();
        }


        /// <summary>
        /// Recursively builds a <see cref="Rule"/> from a <see cref="DynamicRule"/>, compiling conditions and actions.
        /// </summary>
        /// <param name="drule">The dynamic rule definition.</param>
        /// <param name="config">The parsing configuration.</param>
        /// <param name="globalParams">Global parameters for the rule set.</param>
        /// <returns>The constructed <see cref="Rule"/>.</returns>
        private static Rule BuildRuleFromDynamic(DynamicRule drule, ParsingConfig config, List<DynamicRuleParameter> globalParams)
        {

            var rule = new Rule(drule.Id, (rule, inputsDict) =>
            {
                if (inputsDict == null)
                    throw new InvalidOperationException("Expected item to be a dictionary of inputs.");

                var cached = PrepareDynamic(config, globalParams, [.. drule.Parameters], rule, inputsDict, out List<string> keys);

                if (cached.CompiledCond == null)
                {
                    var condLambda = DynamicExpressionParser.ParseLambda(config, [.. cached.ParamExprs], typeof(bool), drule.Condition);
                    cached.CompiledCond = condLambda.Compile();
                }

                var inputValues = keys.Select(k => inputsDict[k]).ToList();

                for (int i = 0; i < cached.ParamEvaluators.Count; i++)
                {
                    var value = cached.ParamEvaluators[i].DynamicInvoke(inputValues.ToArray());
                    inputValues.Add(value);
                }

                cached.InputValues = inputValues;
                rule.Context["_cachedDynamic"] = cached;

                var result = cached.CompiledCond.DynamicInvoke(inputValues.ToArray());

                return result is bool b && b;
            })
            {
                Operator = string.IsNullOrWhiteSpace(drule.Operator) ? "AND" : drule.Operator
            };

            if (drule.DependsOn != null && drule.DependsOn.Any())
            {
                rule.DependsOn(drule.DependsOn.Select(dep =>
                {
                    var parts = dep.Split(':', 2);
                    var ruleId = parts[0];
                    var expected = parts.Length <= 1 || bool.Parse(parts[1]);
                    return (ruleId, expected);
                }).ToArray());
            }

            var scopedParameters = GetScopedParameters(globalParams, drule.Parameters);

            foreach (var childYaml in drule.Rules)
            {
                var childRule = BuildRuleFromDynamic(childYaml, config, scopedParameters);
                rule.Rules.Add(childRule);
            }

            if (!string.IsNullOrWhiteSpace(drule.Then))
            {
                rule.Then((rule, inputsDict, ruleId) =>
                {
                    if (inputsDict == null)
                        throw new InvalidOperationException("Expected item to be a dictionary of inputs.");

                    var cached = PrepareDynamic(config, globalParams, [.. drule.Parameters], rule, inputsDict, out List<string> keys);

                    if (cached.CompiledThen == null)
                    {
                        var thenLambda = DynamicExpressionParser.ParseLambda(config,
                            [.. cached.ParamExprs],
                            typeof(object),
                            drule.Then
                        );
                        cached.CompiledThen = thenLambda.Compile();
                        rule.Context["_cachedDynamic"] = cached;
                    }

                    if (cached.CompiledThen == null || cached.InputValues == null)
                        throw new InvalidOperationException("Compiled then delegate or input values are null.");
                    return cached.CompiledThen.DynamicInvoke(cached.InputValues.ToArray());
                });
            }

            return rule;
        }
    }
}
