using System.Linq.Dynamic.Core.CustomTypeProviders;
using RulesEngine.Core;

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
    
    [DynamicLinqType]
    public static class RiskUtils
    {
        public static string FlagHighRisk(string make, int age, List<DateTime> collisionDates)
        {
            return $"High risk: {make}, Age: {age}, Collisions: {collisionDates.Count}";
        }

        public static string FlagLowRisk(string make, int age)
        {
            return $"Low risk: {make}, Age: {age}";
        }

        public static string FlagReview(string make, List<DateTime> collisionDates)
        {
            return $"Review needed: {make}, Recent collisions: {collisionDates.Count}";
        }

        public static string FlagCriticalReview(string make, int recentCollisionCount)
        {
            return $"Critical review: {make}, Recent collisions: {recentCollisionCount}";
        }
    }
}
