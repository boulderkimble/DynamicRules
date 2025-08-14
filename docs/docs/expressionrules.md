# Expression "Rules of the Road"
Dynamic Rules uses zzzproject's [Dynamic Linq library](https://github.com/zzzprojects/System.Linq.Dynamic.Core) to parse C# expressions into expression trees. There are some particular abilities and limitations to be aware of.

### Expressions with 'new'

In _Then_ or parameter expressions, you cannot use new to produce a typed object - instead use a static utility class.

Use:

```
Then: RiskAssessment.Create(Auto.VIN, "High Risk")
```
instead of:
```
Then: new RiskAssessment(Auto.VIN, "High Risk")
```

You need to be mindful of this when using primitive types like DateTime. So istead of:

```
Expression: new System.DateTime(2025, 8, 1)
```

use:
```
Expression: DateTime.Parse("2025-08-01")
```

However new can be used to return an annonymous object as long as (1) no labels specified and (2) no object initializers the parser can't infer the type from. In practical terms that means rule input object parameters, primitive .NET types, and types you've declared with the ```[DynamicLinqType]``` attribute.

```
Then: "new { Auto.Make, Auto.Year }"
```

### Anonymous Types & Inputs

Inputs can be annoymous types (with or without labels). 

For annonymous inputs you must use the core engine API's (Run(), RunAsync()) that take a dictionary or name value pairs as input because names must be specified matching your YAML/JSON epxressions. The Run() variants wich accept just an enumation of objects will default the paramter names from the type names (which won't be usable in your rule expressions).

Additionally the VS Code extension won't be able to parse and report errors if your code leverages annomyous inputs.

### String Interpolation

C# sting interpolation is not supported in an expression.

### Data Modification

In a rule expression, you can project (select, transform, or create new objects/values from) the input data,
but you cannot modify the original input objects or their properties.

Allowed:
```
Then: "Auto.Make" 
```
Not Allowed:
```
Then: "Auto.Age = 20" 
```
