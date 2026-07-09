using ArcGIS.Core.Events;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Dialogs;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ArcGIS.Desktop.Mapping.Events;
using ConstructionAddIn.Helpers.PointHelpers;
using ConstructionAddIn.Services;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace ConstructionAddIn.Tools.RemoveTools
{
    /// <summary>
    /// Generic delete workflow for construction point fitting features.
    ///
    /// This tool activates rectangle selection, waits until the target layer
    /// has a selected feature, deletes exactly one selected feature from that
    /// target layer, then returns to the Explore tool.
    /// </summary>
    public static class RemovePointFeatureTool
    {
        private static SubscriptionToken _selectionChangedToken;
        private static bool _isRunning;

        public static async Task StartAsync(PointDeleteRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.TargetLayerName))
            {
                MessageBox.Show("Delete request is invalid.", "Delete Feature");
                return;
            }

            PointDeleteContext.CurrentRequest = request;

            await ClearSelectionAsync();

            SubscribeSelectionChanged();

            _isRunning = true;

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
                if (!_isRunning)
                    return;

                var request = PointDeleteContext.CurrentRequest;
                if (request == null)
                    return;

                if (!await TargetLayerHasSelectionAsync(request.TargetLayerName))
                    return;

                _isRunning = false;

                await PointFeatureDeleteService.DeleteSelectedAsync(request);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Delete Tool Exception");
            }
            finally
            {
                if (!_isRunning)
                {
                    await ClearSelectionAsync();
                    await FrameworkApplication.SetCurrentToolAsync("esri_mapping_exploreTool");

                    PointDeleteContext.CurrentRequest = null;
                    UnsubscribeSelectionChanged();
                }
            }
        }

        /// <summary>
        /// Checks only the requested target layer.
        /// This prevents selected Pipes or other layers from interfering with deletion.
        /// </summary>
        private static async Task<bool> TargetLayerHasSelectionAsync(string targetLayerName)
        {
            return await QueuedTask.Run(() =>
            {
                var map = MapView.Active?.Map;
                if (map == null)
                    return false;

                var targetLayer = map.GetLayersAsFlattenedList()
                    .OfType<FeatureLayer>()
                    .FirstOrDefault(layer =>
                        layer.Name.Equals(targetLayerName, StringComparison.OrdinalIgnoreCase));

                if (targetLayer == null)
                    return false;

                return targetLayer.GetSelection().GetCount() > 0;
            });
        }

        private static async Task ClearSelectionAsync()
        {
            await QueuedTask.Run(() =>
            {
                var map = MapView.Active?.Map;
                map?.ClearSelection();
            });
        }

        private static void UnsubscribeSelectionChanged()
        {
            if (_selectionChangedToken == null)
                return;

            MapSelectionChangedEvent.Unsubscribe(_selectionChangedToken);
            _selectionChangedToken = null;
        }
    }
}