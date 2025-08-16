
using RulesEngine.Core;
using RulesEngine.Dynamic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core.CustomTypeProviders;
using System.Runtime.CompilerServices;

namespace CarLot
{

    public class Auto
    {
        public string VIN { get; set; }
        public string Make { get; set; }
        public int Age { get; set; }
        public List<DateTime>? CollisionDates { get; set; } 
    }

    [VSExtensionConfig]
    public static class CarlotVSExtensionMappings
    {
        public static IEnumerable<(string? Name, Type? Type)> ReturnInputMappings()
        {
            // null here means use the type's short name - i.e. "Auto"
            // For primitive and generic types, always specify an explicit name
            // and ensure it's the same name you use in your rules YAML or 
            // JSON expressions
            yield return (null, typeof(Auto));
        }
    }

    [DynamicLinqType]
    public record RiskAssessment(string VIN, string Details)
    {
        public static RiskAssessment Create(string vin, string message)
            => new RiskAssessment(vin, message);
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