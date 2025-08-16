using System.Linq.Dynamic.Core.CustomTypeProviders;
using RulesEngine.Core;
using RulesEngine.Dynamic;

namespace YamlValidatorTest
{
    public class TestItem
    {
        public int Value { get; set; }
        public bool Flag { get; set; }
    }

    public class Auto
    {
        public string Make { get; set; }
        public int Age { get; set; }
        public List<DateTime> CollisionDates { get; set; } = new();
    }

    [VSExtensionConfig]
    public static class InputMappingForExtension
    {
        public static IEnumerable<(string, Type)> ReturnInputMappings()
        {
            yield return ("Auto", typeof(Auto));
            yield return ("TestItem", typeof(TestItem));
        }
    }

    [DynamicLinqType]
    public static class TestDateUtils
    {
        public static int Age(DateTime birthdate)
        {
            var today = DateTime.Today;
            int age = today.Year - birthdate.Year;
            if (birthdate > today.AddYears(-age)) age--;
            return age;
        }
    }

}
