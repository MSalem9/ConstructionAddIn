using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConstructionAddIn.Services
{
    /// <summary>
    /// Central place for repeated Construction fitting rules.
    /// Keeps diameter-based size/quantity logic out of individual placement services.
    /// </summary>
    public static class ConstructionFittingRulesService
    {
        /// <summary>
        /// Common empty description value used in many fields.
        /// </summary>
        public const string EmptyDescription = " - - - ";

        /// <summary>
        /// Stud bolt size used by Valve-Regulator logic.
        /// Format is based on pipe diameter, with 110 mm fixed length.
        /// </summary>
        public static string GetStudBoltSize110(string diameter)
        {
            return NormalizeDiameter(diameter) switch
            {
                "3" => "5/8\" x 110 mm",
                "4" => "5/8\" x 110 mm",
                "6" => "3/4\" x 110 mm",
                "8" => "3/4\" x 110 mm",
                "10" => "7/8\" x 110 mm",
                "12" => "7/8\" x 110 mm",
                "16" => "7/8\" x 110 mm",

                "90" => "5/8\" x 110 mm",
                "125" => "5/8\" x 110 mm",
                "180" => "3/4\" x 110 mm",
                "250" => "3/4\" x 110 mm",
                "315" => "7/8\" x 110 mm",
                "355" => "7/8\" x 110 mm",
                _ => null
            };
        }

        /// <summary>
        /// Stud bolt size used by BallValve/Vent related logic.
        /// Format is based on pipe diameter, with 120 mm fixed length.
        /// </summary>
        public static string GetStudBoltSize120(string diameter)
        {
            return NormalizeDiameter(diameter) switch
            {
                "3" => "5/8\" x 120 mm",
                "4" => "5/8\" x 120 mm",
                "6" => "3/4\" x 120 mm",
                "8" => "3/4\" x 120 mm",
                "10" => "7/8\" x 120 mm",
                "12" => "7/8\" x 120 mm",
                "16" => "7/8\" x 120 mm",
               
                "90" => "5/8\" x 120 mm",
                "125" => "5/8\" x 120 mm",
                "180" => "3/4\" x 120 mm",
                "250" => "3/4\" x 120 mm",
                "315" => "7/8\" x 120 mm",
                "355" => "7/8\" x 120 mm",
                _ => null
            };
        }

        /// <summary>
        /// Common stud bolt quantity rule:
        /// 3  -> 8
        /// 6,8 -> 16
        /// 10,12 -> 24
        /// </summary>
        public static int? GetStudBoltQuantityByDiameter(string diameter)
        {
            return NormalizeDiameter(diameter) switch
            {
                "3" => 8,
                "4" => 8,
                "6" => 16,
                "8" => 16,
                "10" => 24,
                "12" => 24,

                "90" => 8,
                "125" => 8,
                "180" => 16,
                "250" => 16,
                "315" => 24,
                "355" => 24,
                _ => null
            };
        }

        /// <summary>
        /// Normalizes diameter values coming from pipe fields.
        /// Examples:
        /// 3"
        /// 3 inch
        /// 3.0
        /// all become 3.
        /// </summary>
        public static string NormalizeDiameter(string diameter)
        {
            if (string.IsNullOrWhiteSpace(diameter))
                return null;

            var value = diameter
                .Replace("\"", "")
                .Replace("inch", "", System.StringComparison.OrdinalIgnoreCase)
                .Replace("in", "", System.StringComparison.OrdinalIgnoreCase)
                .Trim();

            if (decimal.TryParse(value, out var number))
                return ((int)number).ToString();

            return value;
        }
    }
}
