# Visual Studio Code Rule Editing Extension
To use, configure the extension to load one or more assemblies containing objects marked with specific attributes to tell the extension parser about your rule inputs and parameter types.

```
{
    "rulevalidator.assemblyPaths": "C:\\YamlValidatorTest\\bin\\Debug\\net8.0\\YamlValidatorTest.dll"
    "rulevalidator.fileNamePattern": "rules_.*\\.(yaml|json)$"
}
```

_assemblyPaths_ (comman delineated) must define assemblies (typically 1) with: 

1. Input types defined by a static class marked with the ```VSExtensionConfig``` attribute.

    - Apply this attribute to a static class.
    - Implement the following method:

        `public static IEnumerable<(string, Type)> ReturnInputMappings()`
    
    - The method must yield all input parameter names and types used with your rules engine.
    - The order does not matter.

2. Other classes marked with ```DynamicLinqType```. As [discussed](using.md), this type defines additional types referenced in your rules and it is needed by the rules engine itself as well as the extension.

```
    // Note if you don't use the VS extension, you don't need this.
    [VSExtensionConfig]
    public static class MyRulesInputMapping
    {
        public static IEnumerable<(string? Name, Type? Type)> ReturnInputMapForExtension()
        {
            yield return ("Auto", typeof(Auto));
            yield return ("Collisions", typeof(DbSet<Collision>));
        }
    }

    // These are types referenced by the rules engine typically in parameter expressions 
    // or Then expressions to provide processing utility or define a returned object type.
    // This is required by both the rules engine as well as the VS code extension. The attribute
    // is defined in the Dynamic Rules package used by the rules library.
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

```

# Installing

Coming...

