using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Editing;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ConstructionAddIn.Helpers.PointHelpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ConstructionAddIn.Services
{
    /// <summary>
    /// Generic service responsible for creating construction point features.
    ///
    /// This service should be used by all fitting types instead of creating
    /// separate creation logic for every feature.
    ///
    /// The feature-specific values are supplied through PointDrawRequest:
    /// - TargetLayerName
    /// - static Attributes
    /// - dynamic BeforeCreateAttributes callback
    /// - optional ApplySymbologyAction
    /// </summary>
    public static class PointFeatureCreationService
    {
        /// <summary>
        /// Creates one point feature at the clicked map point using the supplied request.
        ///
        /// Returns false if creation is cancelled or fails.
        /// </summary>
        public static async Task<bool> CreateAsync(MapPoint mapPoint, PointDrawRequest request)
        {
            if (mapPoint == null || request == null || string.IsNullOrWhiteSpace(request.TargetLayerName))
                return false;

            return await QueuedTask.Run(async () =>
            {
                var map = MapView.Active?.Map;
                if (map == null)
                    return false;

                var targetLayer = FindFeatureLayer(map, request.TargetLayerName);
                if (targetLayer == null)
                    return false;

                var attributes = new Dictionary<string, object>(request.Attributes ?? new());

                if (request.BeforeCreateAttributes != null)
                {
                    var dynamicAttributes = await request.BeforeCreateAttributes(mapPoint);

                    // Returning null from placement logic means cancel creation.
                    if (dynamicAttributes == null)
                        return false;

                    foreach (var item in dynamicAttributes)
                        attributes[item.Key] = item.Value;
                }

                attributes["SHAPE"] = mapPoint;

                var editableAttributes = FilterEditableAttributes(targetLayer, attributes);

                var editOperation = new EditOperation
                {
                    Name = $"Create {request.TargetLayerName}",
                    SelectNewFeatures = false
                };

                editOperation.Create(targetLayer, editableAttributes);

                bool success = editOperation.Execute();

                if (!success)
                    return false;

                request.ApplySymbologyAction?.Invoke(targetLayer);

                return true;
            });
        }

        /// <summary>
        /// Finds a feature layer by name in the active map.
        /// Layer name comparison is case-insensitive.
        /// </summary>
        private static FeatureLayer FindFeatureLayer(Map map, string layerName)
        {
            return map.GetLayersAsFlattenedList()
                .OfType<FeatureLayer>()
                .FirstOrDefault(layer =>
                    layer.Name.Equals(layerName, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Removes attributes that cannot be edited or do not exist in the target layer.
        ///
        /// This prevents EditOperation.Create from failing because of invalid fields.
        /// </summary>
        private static Dictionary<string, object> FilterEditableAttributes(
            FeatureLayer targetLayer,
            Dictionary<string, object> attributes)
        {
            var result = new Dictionary<string, object>();

            using var table = targetLayer.GetTable();
            var definition = table.GetDefinition();

            var editableFields = definition.GetFields()
                .Where(field =>
                    field.IsEditable ||
                    field.FieldType == FieldType.Geometry)
                .Select(field => field.Name)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var attribute in attributes)
            {
                if (attribute.Key.Equals("SHAPE", StringComparison.OrdinalIgnoreCase))
                {
                    result[attribute.Key] = attribute.Value;
                    continue;
                }

                if (editableFields.Contains(attribute.Key))
                    result[attribute.Key] = attribute.Value;
            }

            return result;
        }
    }
}