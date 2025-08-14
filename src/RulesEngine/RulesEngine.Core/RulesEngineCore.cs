namespace RulesEngine.Core
{
    public class RulesEngineCore
    {
        private readonly Dictionary<string, Rule> _rules = new();
        private readonly List<Artifact> _artifacts = new();
        public IReadOnlyList<Artifact> Artifacts => _artifacts.AsReadOnly();
        public event Action<Artifact>? ArtifactAdded;

        public void ClearArtifacts()
        {
            _artifacts.Clear();
        }

        public List<string> Run(params object[] parameters)
        {
            // Note rule parameter names will be set to their type name, thus you
            // should only use this if all parameters are user defined types AND
            // you've written your rules to use the type name as the parameter name.
            if (parameters == null || parameters.Length == 0)
                throw new ArgumentException("Parameters cannot be null or empty.");
            var ruleInputs = parameters.ToList();
            return Run(ruleInputs);
        }

        public List<string> Run(IList<object> parameters)
        {
            // Note rule parameter names will be set to their type name, thus you
            // should only use this if all parameters are user defined types AND
            // you've written your rules to use the type name as the parameter name.
            if (parameters == null || parameters.Count == 0)
                throw new ArgumentException("Parameters cannot be null or empty.");
            var ruleInputs = new Dictionary<string, object>();
            foreach (var param in parameters)
            {
                var type = param.GetType();
                if (IsAnonymousType(type))
                {
                    throw new ArgumentException($"Anonymous type detected in parameters list. Use the Run method with a dictionary to provide parameter names for anonymous types.");
                }
                ruleInputs[type.Name] = param;
            }
            if (ruleInputs.Count == 0)
            {
                throw new ArgumentException("No valid rule inputs found.");
            }
            // Run the rules engine with the provided parameters
            return Run(ruleInputs);
        }

        public List<string> Run<T>(IList<T> parameters)
        {
            // Note rule parameter names will be set to their type name, thus you
            // should only use this if all parameters are user defined types AND
            // you've written your rules to use the type name as the parameter name.
            if (parameters == null || parameters.Count == 0)
                throw new ArgumentException("Parameters cannot be null or empty.");

            // Convert to object list for internal processing
            var objectParams = parameters.Cast<object>().ToList();
            return Run(objectParams);
        }

        public async Task<List<string>> RunAsync(IList<object> parameters, int maxConcurrency = 0)
        {
            // Note rule parameter names will be set to their type name, thus you
            // should only use this if all parameters are user defined types AND
            // you've written your rules to use the type name as the parameter name.
            if (parameters == null || parameters.Count == 0)
                throw new ArgumentException("Parameters cannot be null or empty.");
            var ruleInputs = new Dictionary<string, object>();
            foreach (var param in parameters)
            {
                var type = param.GetType();
                if (IsAnonymousType(type))
                {
                    throw new ArgumentException($"Anonymous type detected in parameters list. Use the RunAsync method with a dictionary to provide parameter names for anonymous types.");
                }
                ruleInputs[type.Name] = param;
            }
            if (ruleInputs.Count == 0)
            {
                throw new ArgumentException("No valid rule inputs found.");
            }
            // Run the rules engine with the provided parameters
            return await RunAsync(ruleInputs, maxConcurrency);
        }

        /// <summary>
        /// Executes all rules synchronously, respecting rule dependencies.
        /// Returns a list of error messages for any rules that fail or have unsatisfied dependencies.
        /// </summary>
        /// <param name="parameters">Input parameters for rule evaluation, keyed by parameter name.</param>
        /// <returns>A list of error messages for failed rules.</returns>
        public List<string> Run(IDictionary<string, object> parameters)
        {
            var errors = new List<string>();
            var results = new Dictionary<string, bool>();
            var sortedRules = GetRulesInDependencyOrder();

            foreach (var rule in sortedRules)
            {
                bool dependentsSuccessful = rule.DependentRules.All(dep => results.TryGetValue(dep.RuleId, out var actual) && actual == dep.ExpectedResult);
                bool ruleResult = dependentsSuccessful && rule.Run(parameters);
                results[rule.Id] = ruleResult;
                if (ruleResult)
                {
                    var artifact = new Artifact(rule.RunSuccess(parameters, rule.Id), rule.Id);
                    _artifacts.Add(artifact);
                    ArtifactAdded?.Invoke(artifact);
                }
                else
                {
                    if (!dependentsSuccessful)
                        errors.Add($"Rule '{rule.Id}' failed due to unsatisfied dependencies: {string.Join(", ", rule.DependentRules)}.");
                    else
                        errors.Add($"Rule '{rule.Id}' failed.");
                }
            }
            return errors;
        }

        public List<string> Run(params (string key, object value)[] parameters)
        {
            var dict = new Dictionary<string, object>();
            foreach (var (key, value) in parameters)
            {
                if (string.IsNullOrWhiteSpace(key))
                    throw new ArgumentException("Parameter name cannot be null or empty.", nameof(key));
                if (dict.ContainsKey(key))
                    throw new ArgumentException($"Duplicate parameter name detected: '{key}'", nameof(key));
                dict[key] = value;
            }
            return Run(dict);
        }

        /// <summary>
        /// Executes all rules asynchronously respecting rule dependencies and allowing concurrent execution.
        /// Rules are grouped by dependency level and processed in parallel with max concurrency set to
        /// <paramref name="maxConcurrency"/>.
        /// Returns a list of error messages for any rules that fail or have unsatisfied dependencies. For anything
        /// but large rule sets, this method will likely be slower than the synchronous version.
        /// </summary>
        /// <param name="parameters">Input parameters for rule evaluation, keyed by parameter name.</param>
        /// <param name="maxConcurrency">
        /// Maximum number of rules to execute concurrently. If set to 0, uses the number of processor cores.
        /// </param>
        /// <returns>
        /// A task that resolves to a list of error messages for failed rules.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// Thrown if an anonymous type is provided without a parameter name.
        /// </exception>
        public async Task<List<string>> RunAsync(IDictionary<string, object> parameters, int maxConcurrency = 0)
        {
            var errors = new List<string>();
            var results = new Dictionary<string, bool>();
            var allRules = FlattenRulesToDictionary(_rules.Values);
            if (maxConcurrency <= 0)
                maxConcurrency = Environment.ProcessorCount;

            var paramCopy = new Dictionary<string, object>(parameters);

            foreach (var key in paramCopy.Keys.ToList())
            {
                if (string.IsNullOrEmpty(key))
                {
                    if (IsAnonymousType(paramCopy[key].GetType()))
                    {
                        throw new ArgumentException("For anonymous types, a parameter name must be provided.");
                    }
                    else
                    {
                        paramCopy[paramCopy[key].GetType().Name] = paramCopy[key];
                        paramCopy.Remove(key);
                    }
                }
            }

            // Assign levels to rules
            var ruleLevels = new Dictionary<string, int>();
            // Find teh maximum dependency level in the rule graph
            int GetLevel(Rule rule)
            {
                if (rule.DependentRules.Count == 0) return 0;
                return 1 + rule.DependentRules.Max(dep => GetLevel(allRules[dep.RuleId]));
            }
            foreach (var rule in allRules.Values)
                ruleLevels[rule.Id] = GetLevel(rule);

            // Group rules by level
            var levels = ruleLevels.GroupBy(kvp => kvp.Value).OrderBy(g => g.Key)
                .Select(g => g.Select(kvp => allRules[kvp.Key]).ToList()).ToList();

            var semaphore = new SemaphoreSlim(maxConcurrency);

            // For each dependency level (lowest to highest), run the rules concurrently

            foreach (var levelRules in levels)
            {
                var tasks = levelRules.Select(rule =>
                    Task.Run(async () =>
                    {
                        await semaphore.WaitAsync();
                        try
                        {
                            bool dependentsSuccessful = rule.DependentRules.All(dep => results.TryGetValue(dep.RuleId, out var actual) && actual == dep.ExpectedResult);
                            bool ruleResult = dependentsSuccessful && rule.Run(paramCopy);
                            lock (results)
                            {
                                results[rule.Id] = ruleResult;
                                if (ruleResult)
                                {
                                    var artifact = new Artifact(rule.RunSuccess(paramCopy, rule.Id), rule.Id);
                                    _artifacts.Add(artifact);
                                    ArtifactAdded?.Invoke(artifact);
                                }
                                else
                                {
                                    if (!dependentsSuccessful)
                                        errors.Add($"Rule '{rule.Id}' failed due to unsatisfied dependencies: {string.Join(", ", rule.DependentRules)}.");
                                    else
                                        errors.Add($"Rule '{rule.Id}' failed.");
                                }
                            }
                        }
                        finally
                        {
                            semaphore.Release();
                        }
                    })
                ).ToArray();

                await Task.WhenAll(tasks);
            }

            return errors;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RulesEngineCore"/> class with the provided rules.
        /// </summary>
        /// <param name="rules">A collection of rules to add to the engine.</param>
        public RulesEngineCore(IEnumerable<Rule> rules)
        {
            foreach (var rule in rules)
            {
                AddRule(rule);
            }
        }

        /// <summary>
        /// Adds a rule to the engine. Throws if a rule with the same ID already exists.
        /// </summary>
        /// <param name="rule">The rule to add.</param>
        /// <returns>The current <see cref="RulesEngineCore"/> instance.</returns>
        public RulesEngineCore AddRule(Rule rule)
        {
            if (_rules.ContainsKey(rule.Id))
                throw new InvalidOperationException($"Rule with ID '{rule.Id}' already exists.");
            _rules[rule.Id] = rule;
            return this;
        }

        /// <summary>
        /// Recursively flattens a collection of rules and their children into a dictionary keyed by rule ID.
        /// </summary>
        /// <param name="rules">The collection of rules to flatten.</param>
        /// <returns>A dictionary of all rules, including nested child rules, keyed by rule ID.</returns>
        private static Dictionary<string, Rule> FlattenRulesToDictionary(IEnumerable<Rule> rules)
        {
            var dict = new Dictionary<string, Rule>();
            void AddRuleRecursive(Rule rule)
            {
                if (dict.ContainsKey(rule.Id))
                    return;
                dict[rule.Id] = rule;
                foreach (var child in rule.Rules)
                    AddRuleRecursive(child);
            }
            foreach (var rule in rules)
                AddRuleRecursive(rule);
            return dict;
        }


        /// <summary>
        /// Returns all rules in dependency order, ensuring dependencies are processed first.
        /// Throws if circular dependencies are detected.
        /// </summary>
        /// <returns>A list of rules sorted by dependency order.</returns>
        private List<Rule> GetRulesInDependencyOrder()
        {
            var depthFirstRules = new List<Rule>();
            var traversed = new HashSet<string>();
            var traversing = new HashSet<string>();
            var allRules = FlattenRulesToDictionary(_rules.Values);

            void Traverse(string rid)
            {
                if (traversed.Contains(rid)) return;
                if (traversing.Contains(rid)) throw new InvalidOperationException($"Circular dependency detected for rule '{rid}'.");
                if (!allRules.TryGetValue(rid, out var rule)) throw new KeyNotFoundException($"Rule with ID '{rid}' not found.");
                traversing.Add(rid);
                rule.DependentRules.ForEach(depId => Traverse(depId.RuleId));
                traversing.Remove(rid);
                traversed.Add(rid);
                depthFirstRules.Add(rule);
            }

            foreach (var rid in allRules.Keys)
            {
                Traverse(rid);
            }

            return depthFirstRules;
        }
        
        /// <summary>
        /// Determines whether the specified type is an anonymous type.
        /// </summary>
        /// <param name="type">The type to check.</param>
        /// <returns>True if the type is anonymous; otherwise, false.</returns>
        private static bool IsAnonymousType(Type type)
        {
            return Attribute.IsDefined(type, typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute), false)
                && type.IsGenericType
                && type.Name.Contains("AnonymousType");
        }
    }
}




