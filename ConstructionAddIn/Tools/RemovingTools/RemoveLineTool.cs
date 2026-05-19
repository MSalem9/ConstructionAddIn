using ArcGIS.Core.CIM;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Editing;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Dialogs;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
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
                MessageBox.Show("Please draw a valid rectangle.");
                return false;
            }

            var objectIds = await QueuedTask.Run(() =>
            {
                if (MapView.Active?.Map == null)
                    return new List<long>();

                var featureLayer = MapView.Active.Map
                    .GetLayersAsFlattenedList()
                    .OfType<FeatureLayer>()
                    .FirstOrDefault(l => l.Name == TargetLayerName);

                if (featureLayer == null)
                    return new List<long>();

                if (featureLayer.ShapeType != esriGeometryType.esriGeometryPolyline)
                    return new List<long>();

                var filter = new SpatialQueryFilter
                {
                    FilterGeometry = geometry,
                    SpatialRelationship = SpatialRelationship.Intersects
                };

                var ids = new List<long>();

                using (RowCursor cursor = featureLayer.Search(filter))
                {
                    while (cursor.MoveNext())
                    {
                        using var feature = cursor.Current as Feature;
                        if (feature != null)
                            ids.Add(feature.GetObjectID());
                    }
                }

                return ids;
            });

            if (objectIds.Count == 0)
            {
                MessageBox.Show("No lines were found inside the selected area.");
                return false;
            }

            var result = MessageBox.Show(
                $"Are you sure you want to delete {objectIds.Count} line(s)?",
                "Confirm Delete",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Warning);

            if (result != System.Windows.MessageBoxResult.Yes)
                return false;

            bool deleted = await QueuedTask.Run(() =>
            {
                if (MapView.Active?.Map == null)
                    return false;

                var featureLayer = MapView.Active.Map
                    .GetLayersAsFlattenedList()
                    .OfType<FeatureLayer>()
                    .FirstOrDefault(l => l.Name == TargetLayerName);

                if (featureLayer == null)
                {
                    MessageBox.Show($"Layer '{TargetLayerName}' was not found.");
                    return false;
                }

                var operation = new EditOperation
                {
                    Name = $"Delete lines from {TargetLayerName}"
                };

                operation.Delete(featureLayer, objectIds);

                bool ok = operation.Execute();
                if (!ok)
                    MessageBox.Show(operation.ErrorMessage, "Delete failed");

                return ok;
            });

            if (!deleted)
                return false;

            bool saved = await Project.Current.SaveEditsAsync();
            if (!saved)
            {
                MessageBox.Show("Lines were deleted, but saving edits failed.", "Save failed");
                return false;
            }

            MessageBox.Show("Lines deleted successfully.");

            // Return to the default Explore tool.
            _ = FrameworkApplication.SetCurrentToolAsync("esri_mapping_exploreTool");

            return true;
        }
    }

    // This class is used to store the target layer name that the RemoveLineTool will operate on.
    internal static class RemoveLineContext
    {
        public static string TargetLayerName { get; set; }
    }
}
