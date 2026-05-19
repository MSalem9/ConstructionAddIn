using ArcGIS.Desktop.Mapping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConstructionAddIn.Helpers.LineHelpers
{
    internal static class LineToolFieldHelper
    {
        private static readonly HashSet<string> BlockedFields =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "OBJECTID",
                "Shape",
                "Shape_Length",
                "Shape_Area"
            };

        public static Dictionary<string, object> FilterEditableAttributes(
            FeatureLayer featureLayer,
            IDictionary<string, object> inputAttributes)
        {
            var result = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

            if (featureLayer == null || inputAttributes == null || inputAttributes.Count == 0)
                return result;

            using var table = featureLayer.GetTable();
            var definition = table.GetDefinition();
            var fields = definition.GetFields();

            foreach (var kvp in inputAttributes)
            {
                if (string.IsNullOrWhiteSpace(kvp.Key))
                    continue;

                if (BlockedFields.Contains(kvp.Key))
                    continue;

                var field = fields.FirstOrDefault(f =>
                    f.Name.Equals(kvp.Key, StringComparison.OrdinalIgnoreCase));

                if (field == null)
                    continue;

                if (!field.IsEditable)
                    continue;

                result[field.Name] = kvp.Value;
            }

            return result;
        }
    }
}
