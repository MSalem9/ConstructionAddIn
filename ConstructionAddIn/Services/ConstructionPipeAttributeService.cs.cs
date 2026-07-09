using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace ConstructionAddIn.Services
{
    /// <summary>
    /// Finds the pipe under the clicked point and reads common pipe attributes.
    /// Used by all Construction fitting placement services.
    /// </summary>
    public static class ConstructionPipeAttributeService
    {
        public class PipeInfo
        {
            public FeatureLayer Layer { get; set; }
            public Feature Feature { get; set; }
            public Geometry Geometry { get; set; }
            public string Diameter { get; set; }
            public string PipeName { get; set; }
            public string ProjectName { get; set; }
        }

        /// <summary>
        /// Searches known pipe layers and returns the first pipe intersecting the clicked point.
        /// Update layer names here if your Construction project uses different pipe layer names.
        /// </summary>
        public static async Task<PipeInfo> FindPipeAtPointAsync(MapPoint mapPoint)
        {
            return await QueuedTask.Run(() =>
            {
                var map = MapView.Active?.Map;
                if (map == null)
                    return null;

                string[] pipeLayerNames =
                {
                    "Pipes",
                    "PePipes",
                    "Steel"
                };

                foreach (var layerName in pipeLayerNames)
                {
                    var layer = map.GetLayersAsFlattenedList()
                        .OfType<FeatureLayer>()
                        .FirstOrDefault(l => l.Name.Equals(layerName, StringComparison.OrdinalIgnoreCase));

                    if (layer == null)
                        continue;

                    var filter = new SpatialQueryFilter
                    {
                        FilterGeometry = mapPoint,
                        SpatialRelationship = SpatialRelationship.Intersects
                    };

                    using var rowCursor = layer.Search(filter);
                    if (!rowCursor.MoveNext())
                        continue;

                    var feature = rowCursor.Current as Feature;
                    if (feature == null)
                        continue;

                    return new PipeInfo
                    {
                        Layer = layer,
                        Feature = feature,
                        Geometry = feature.GetShape(),
                        Diameter = GetValue(feature, "DIAMETER") ?? GetValue(feature, "Diameter") ?? GetValue(feature, "Diam"),
                        PipeName = GetValue(feature, "Name") ?? GetValue(feature, "Name") ?? GetValue(feature, "Name"),
                        ProjectName = GetValue(feature, "ProjectName") ?? GetValue(feature, "Project Name")
                    };
                }

                return null;
            });
        }

        /// <summary>
        /// Safely reads a field value from a feature.
        /// Returns null if the field does not exist or value is empty.
        /// </summary>
        private static string GetValue(Feature feature, string fieldName)
        {
            var table = feature.GetTable();
            var definition = table.GetDefinition();

            bool exists = definition.GetFields()
                .Any(f => f.Name.Equals(fieldName, StringComparison.OrdinalIgnoreCase));

            if (!exists)
                return null;

            var value = feature[fieldName];
            return value == null || value == DBNull.Value ? null : value.ToString();
        }
    }
}