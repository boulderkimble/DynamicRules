# Dynamic Rules Engine

This is a .NET-based rules engine for externally defined rules based on C# LINQ expressions. It currently supports rules in YAML or JSON but could be exanded to support other formats. 

Alpha - No releases yet

### Build Rules Engine
```
dotnet build
```

### Build & Install VS Code Extension

```
dotnet build -c Release
cd /src/RuleVSExtension
npm run build
npx vsce package  
code --install-extension rulevalidator-1.0.0.vsix
```


#### See [Documentation](https://boulderkimble.github.io/DynamicRules/)


![VS Extension](docs/docs/imgs/vseditor.gif)