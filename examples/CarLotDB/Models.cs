
using RulesEngine.Core;
using System.Linq.Dynamic.Core.CustomTypeProviders;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using RulesEngine.Dynamic;
using Microsoft.EntityFrameworkCore;


namespace CarLotDB
{

    public class Auto
    {
        [Key]
        public string VIN { get; set; }
        public string Make { get; set; }
        public int Age { get; set; }
    }

    public class Collision
    {
        public int Id { get; set; }
        public string VIN { get; set; }
        public DateTime Date { get; set; }
    }

    [VSExtensionConfig]
    public static class CarlotVSExtensionMappings
    {
        public static IEnumerable<(string? Name, Type? Type)> ReturnInputMapForExtension()
        {
            yield return ("Auto", typeof(Auto));
            yield return ("Collisions", typeof(DbSet<Collision>));
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