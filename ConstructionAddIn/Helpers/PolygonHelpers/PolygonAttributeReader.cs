using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Mapping;
using ConstructionAddIn.Helpers.LineHelpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConstructionAddIn.Helpers.PolygonHelpers
{
    internal static class PolygonAttributeReader
    {
        public static Dictionary<string, object> ReadFromPolygonAtLineEnd(
            FeatureLayer polygonLayer,
            Polyline line,
            Dictionary<string, string> polygonFieldMappings)
        {
            var result = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

            if (polygonLayer == null || line == null || line.IsEmpty)
                return result;

            if (polygonFieldMappings == null || polygonFieldMappings.Count == 0)
                return result;

            MapPoint endPoint = LineToolGeometryHelper.GetLineEndPoint(line);
            if (endPoint == null)
                return result;

            var filter = new SpatialQueryFilter
            {
                FilterGeometry = endPoint,
                SpatialRelationship = SpatialRelationship.Intersects
            };

            using RowCursor cursor = polygonLayer.Search(filter);

            while (cursor.MoveNext())
            {
                using var feature = cursor.Current as Feature;
                if (feature == null)
                    continue;

                foreach (var map in polygonFieldMappings)
                {
                    string polygonFieldName = map.Key;
                    string targetFieldName = map.Value;

                    try
                    {
                        result[targetFieldName] = feature[polygonFieldName];
                    }
                    catch
                    {
                        // Skip missing/unreadable field
                    }
                }

                // Use first matched polygon only
                break;
            }

            return result;
        }
    }
}
