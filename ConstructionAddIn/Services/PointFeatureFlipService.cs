using ArcGIS.Core.Data;
using ArcGIS.Core.Events;
using ArcGIS.Desktop.Editing;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Dialogs;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ArcGIS.Desktop.Mapping.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ConstructionAddIn.Services
{
    public static class PointFeatureFlipService
    {
        private static SubscriptionToken _selectionChangedToken;
        private static bool _isRunning;
        private static bool _isFlipping;

        public static async Task StartAsync()
        {
            _isRunning = true;
            _isFlipping = false;

            await ClearSelectionAsync();

            SubscribeSelectionChanged();

            await FrameworkApplication.SetCurrentToolAsync("esri_mapping_selectByRectangleTool");
        }

        private static void SubscribeSelectionChanged()
        {
            if (_selectionChangedToken != null)
                return;

            _selectionChangedToken = MapSelectionChangedEvent.Subscribe(OnMapSelectionChanged);
        }

        private static async void OnMapSelectionChanged(MapSelectionChangedEventArgs args)
        {
            try
            {
                if (!_isRunning || _isFlipping)
                    return;

                if (!await AnyFittingSelectionAsync())
                    return;

                _isFlipping = true;
                _isRunning = false;

                await FlipSelectedFeaturesAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Flip Tool Exception");
            }
            finally
            {
                if (!_isRunning)
                {
                    await ClearSelectionAsync();
                    await FrameworkApplication.SetCurrentToolAsync("esri_mapping_exploreTool");
                    UnsubscribeSelectionChanged();
                    _isFlipping = false;
                }
            }
        }

        private static async Task FlipSelectedFeaturesAsync()
        {
            await QueuedTask.Run(() =>
            {
                var map = MapView.Active?.Map;
                if (map == null)
                    return;

                var selectedLayers = map.GetLayersAsFlattenedList()
                    .OfType<FeatureLayer>()
                    .Where(layer => layer.GetSelection().GetCount() > 0)
                    .ToList();

                if (selectedLayers.Count == 0)
                {
                    MessageBox.Show("Please select at least one fitting feature.", "Flip Feature");
                    return;
                }

                var operation = new EditOperation
                {
                    Name = "Flip Construction Fitting"
                };

                var modifiedCount = 0;

                foreach (var layer in selectedLayers)
                {
                    using var table = layer.GetTable();
                    var definition = table.GetDefinition();

                    var hasAngleField = definition.GetFields()
                        .Any(field => field.Name.Equals("ANGLE", StringComparison.OrdinalIgnoreCase));

                    if (!hasAngleField)
                        continue;

                    var selectedIds = layer.GetSelection()
                        .GetObjectIDs()
                        .Select(id => Convert.ToInt64(id))
                        .ToArray();

                    if (selectedIds.Length == 0)
                        continue;

                    using var cursor = table.Search(new QueryFilter
                    {
                        ObjectIDs = selectedIds
                    }, false);

                    while (cursor.MoveNext())
                    {
                        using var row = cursor.Current;

                        var objectId = Convert.ToInt64(row.GetObjectID());

                        var currentAngle = 0.0;

                        if (row["ANGLE"] != null && row["ANGLE"] != DBNull.Value)
                            currentAngle = Convert.ToDouble(row["ANGLE"]);

                        var flippedAngle = NormalizeAngle(currentAngle + 180);

                        operation.Modify(layer, objectId, new Dictionary<string, object>
                        {
                            { "ANGLE", flippedAngle }
                        });

                        modifiedCount++;
                    }
                }

                if (modifiedCount == 0)
                {
                    MessageBox.Show("No selected fitting with ANGLE field was found.", "Flip Feature");
                    return;
                }

                if (!operation.Execute())
                {
                    MessageBox.Show(operation.ErrorMessage, "Flip Feature Failed");
                }
            });
        }

        private static async Task<bool> AnyFittingSelectionAsync()
        {
            return await QueuedTask.Run(() =>
            {
                var map = MapView.Active?.Map;
                if (map == null)
                    return false;

                return map.GetLayersAsFlattenedList()
                    .OfType<FeatureLayer>()
                    .Any(layer => layer.GetSelection().GetCount() > 0);
            });
        }

        private static async Task ClearSelectionAsync()
        {
            await QueuedTask.Run(() =>
            {
                MapView.Active?.Map?.ClearSelection();
            });
        }

        private static void UnsubscribeSelectionChanged()
        {
            if (_selectionChangedToken == null)
                return;

            MapSelectionChangedEvent.Unsubscribe(_selectionChangedToken);
            _selectionChangedToken = null;
        }

        private static double NormalizeAngle(double angle)
        {
            angle %= 360;

            if (angle < 0)
                angle += 360;

            return angle;
        }
    }
}