using Xunit;
using System.Diagnostics;
using System.IO;

namespace RulesEngine.Yaml.Tests;

public class YamlValidatorTests
{
  private string ValidatorExe => Path.GetFullPath(
      Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "src", "RuleVSExtension", "cli", "Release", "net8.0", "RuleValidator.exe"));

  private string WriteTempFile(string content, string ext = "yaml")
  {
    var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + "." + ext);
    File.WriteAllText(path, content);
    return path;
  }

  [Fact]
  public void ValidYamlWithType_AllExpressionsValid()
  {
    // Arrange: YAML and test type  
    var yaml = @"  
Parameters:
  - Name: Param1
    Expression: TestItems.Flag 
Rules:
  - Id: rule_positive  
  - Parameters:
      - Name: InnerParam1
        Expression: Param1 < 10
      - Name: InnerParam2
        Expression: InnerParam1 == true
    Condition: TestItem.Value > 0 && Param1 == 10
    Then: TestItem.Flag.ToString()
";
    var yamlPath = WriteTempFile(yaml);

    var assemblyPath = Path.Combine(AppContext.BaseDirectory, "YamlValidatorTest.dll");

    // Act  
    var psi = new ProcessStartInfo(ValidatorExe, $"\"{yamlPath}\" \"{assemblyPath}\"")
    {
      RedirectStandardOutput = true,
      RedirectStandardError = true,
      UseShellExecute = false
    };
    var proc = Process.Start(psi);
    proc.WaitForExit();

    var output = proc.StandardOutput.ReadToEnd();
    var error = proc.StandardError.ReadToEnd();

    // Assert  
    Assert.Equal(0, proc.ExitCode);
    Assert.Contains("All expressions are valid.", output);
  }

  [Fact]
  public void InvalidYaml_SyntaxError_ReturnsError()
  {
    var yaml = @"  
Rules:
  - Id: rule1
    Condition: TestItem != null
";
    var yamlPath = WriteTempFile(yaml);

    var assemblyPath = Path.Combine(AppContext.BaseDirectory, "YamlValidatorTest.dll");

    var psi = new ProcessStartInfo(ValidatorExe, $"\"{yamlPath}\" \"{assemblyPath}\"")
    {
      RedirectStandardOutput = true,
      RedirectStandardError = true,
      UseShellExecute = false
    };
    var proc = Process.Start(psi);
    proc.WaitForExit();

    var output = proc.StandardOutput.ReadToEnd();
    var error = proc.StandardError.ReadToEnd();

    Assert.Equal(3, proc.ExitCode);
    Assert.Contains("Condition error", output);
  }
}