using System;
using System.Collections.Generic;

namespace ConstructionAddIn.Helpers.LineHelpers
{
    internal static class LineAttributeMergeHelper
    {
        public static Dictionary<string, object> MergeAttributes(
            IDictionary<string, object> baseAttributes,
            IDictionary<string, object> polygonAttributes,
            bool polygonOverridesExisting)
        {
            var result = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

            if (baseAttributes != null)
            {
                foreach (var kvp in baseAttributes)
                    result[kvp.Key] = kvp.Value;
            }

            if (polygonAttributes != null)
            {
                foreach (var kvp in polygonAttributes)
                {
                    if (polygonOverridesExisting || !result.ContainsKey(kvp.Key))
                        result[kvp.Key] = kvp.Value;
                }
            }

            return result;
        }
    }
}
