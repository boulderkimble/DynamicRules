namespace RulesEngine.Dynamic
{
/// <summary>
/// Marks a static class to provide the Dynamic Rules VS Code extension with information
/// about rule engine inputs. This is necessary so that the extension can parse and 
/// validate rules expressions. It is NOT required for running the rules engine itself.
///
/// Usage:
/// - Apply this attribute to a static class.
/// - Implement the following method:
///     public static IEnumerable<(string, Type)> ReturnInputMappings()
/// - The method must yield all input parameter names and types used with your rules engine.
///   The order does not matter.
/// - The Dynamic Rules VS Extension assemblyPaths setting must contain a path
///   to the assembly containing this class. See the extension documentation.
///
/// Example:
/// [VSExtensionConfig]
/// public static class InputMappingForExtension
/// {
///     public static IEnumerable<(string, Type)> ReturnInputMappings()
///     {
///         yield return ("Auto", typeof(Auto));
///         yield return ("Collisions", typeof(DbSet<Collision>));
///     }
/// }
///
/// Notes:
/// - Parameter names must match those used at runtime.
/// - If the parameter name is null or empty, the extension will use the type name
///   which is only useful for a user defined class assuming you use it with that
///   name in your code and YAML/JSON rules. For primitive or complex generic types
///   (e.g. List<string>), always specify an explicit name.
/// - As an example consider the above InputMappingForExtension class, your code
///   should invoke the rules engine with something like this:
/// 
///     processor.CoreEngine.Run(new Dictionary<string, object> {
///         { "Auto", car },
///         { "Collisions", dbContext.Collisions }
///     });
/// 
///   And likewise you would have rule expressions (e.g. YAML) like this:
/// 
///     - Id: RecentCollision
///       Expression: Collisions.Where(c => c.VIN == Auto.VIN && c.Date > DateTime.Now.AddDays(-30)).Any()
/// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public class VSExtensionConfigAttribute : Attribute
    {

    }

}