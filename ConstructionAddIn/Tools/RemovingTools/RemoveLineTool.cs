using ArcGIS.Core.CIM;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Editing;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Dialogs;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ConstructionAddIn.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ConstructionAddIn.Tools.RemovingTools
{
    internal class RemoveLineTool : MapTool
    {
        private string TargetLayerName;

        public RemoveLineTool()
        {
            IsSketchTool = true;
            SketchType = SketchGeometryType.Rectangle;
            SketchOutputMode = SketchOutputMode.Map;
        }

        protected override Task OnToolActivateAsync(bool active)
        {
            TargetLayerName = RemoveLineContext.TargetLayerName;
            return Task.CompletedTask;
        }

        protected override async Task<bool> OnSketchCompleteAsync(Geometry geometry)
        {
            if (geometry == null || geometry.IsEmpty)
            {
                MessageBox.Show("Please draw a valid rectangle.", "Select Area");
                return false;
            }

            // Collect only Object IDs within the sketch geometry
            var objectIds = await QueuedTask.Run(() =>
            {
                var ids = new List<long>();
                var map = MapView.Active?.Map;
                if (map == null)
                    return ids;

                var featureLayer = map.GetLayersAsFlattenedList()
                    .OfType<FeatureLayer>()
                    .FirstOrDefault(l => l.Name.Equals(TargetLayerName, StringComparison.OrdinalIgnoreCase));

                if (featureLayer == null || featureLayer.ShapeType != esriGeometryType.esriGeometryPolyline)
                    return ids;

                var filter = new SpatialQueryFilter
                {
                    FilterGeometry = geometry,
                    SpatialRelationship = SpatialRelationship.Intersects
                };

                using var cursor = featureLayer.Search(filter);
                while (cursor.MoveNext())
                {
                    using var f = cursor.Current as Feature;
                    if (f != null)
                        ids.Add(f.GetObjectID());
                }
                return ids;
            });

            if (objectIds.Count == 0)
            {
                MessageBox.Show("No lines were found inside the selected area.", "Delete Lines");
                return false;
            }

            // Centralized validation, creation-date rule, and supervisor passcode check
            var authResult = await FeatureDeletionGuardService.EvaluateAndAuthorizeByLayerAsync(
                TargetLayerName,
                objectIds,
                maxDaysAllowed: 14);

            if (!authResult.IsAuthorized)
                return false;

            var confirmResult = MessageBox.Show(
                $"Are you sure you want to delete {objectIds.Count} line(s)?",
                "Confirm Delete",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Warning);

            if (confirmResult != System.Windows.MessageBoxResult.Yes)
                return false;

            bool deleted = await QueuedTask.Run(() =>
            {
                var featureLayer = MapView.Active?.Map?
                    .GetLayersAsFlattenedList()
                    .OfType<FeatureLayer>()
                    .FirstOrDefault(l => l.Name.Equals(TargetLayerName, StringComparison.OrdinalIgnoreCase));

                if (featureLayer == null)
                {
                    MessageBox.Show($"Layer '{TargetLayerName}' was not found.", "Layer Missing");
                    return false;
                }

                var operation = new EditOperation
                {
                    Name = $"Delete lines from {TargetLayerName} by {FeatureDeletionGuardService.GetCurrentOperatorName()}"
                };

                operation.Delete(featureLayer, objectIds);

                bool ok = false;
                try
                {
                    DeletionController.IsAddInDeleting = true;
                    ok = operation.Execute();
                }
                finally
                {
                    DeletionController.IsAddInDeleting = false;
                }

                if (!ok)
                    MessageBox.Show(operation.ErrorMessage, "Delete Failed");

                return ok;
            });

            if (!deleted)
                return false;

            bool saved = await Project.Current.SaveEditsAsync();
            if (!saved)
            {
                MessageBox.Show("Lines were deleted, but saving edits failed.", "Save Failed");
                return false;
            }

            // Archive to Documents\ArcGIS\Archive if supervisor override was required
            if (authResult.BypassUsed && authResult.GeoJsonSnapshots.Count > 0)
            {
                try
                {
                    FeatureDeletionGuardService.ArchiveFeatures(authResult.GeoJsonSnapshots);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Features were deleted, but archival failed: {ex.Message}", "Archive Warning");
                }
            }

            MessageBox.Show("Lines deleted successfully.", "Delete Complete");

            _ = FrameworkApplication.SetCurrentToolAsync("esri_mapping_exploreTool");
            return true;
        }
    }

    internal static class RemoveLineContext
    {
        public static string TargetLayerName { get; set; }
    }
}