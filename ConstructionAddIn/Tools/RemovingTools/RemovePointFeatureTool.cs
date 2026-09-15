using ArcGIS.Core.Events;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Dialogs;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ArcGIS.Desktop.Mapping.Events;
using ConstructionAddIn.Helpers.PointHelpers;
using ConstructionAddIn.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ConstructionAddIn.Tools.RemoveTools
{
    /// <summary>
    /// Generic delete workflow for construction point fitting features.
    /// Intercepts rectangle selections, runs creation-date validation and supervisor
    /// bypass logic, deletes through PointFeatureDeleteService, and archives old features.
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

                // Extract only Object IDs from the target layer selection
                var objectIds = await QueuedTask.Run(() =>
                {
                    var map = MapView.Active?.Map;
                    if (map == null)
                        return new List<long>();

                    var targetLayer = map.GetLayersAsFlattenedList()
                        .OfType<FeatureLayer>()
                        .FirstOrDefault(l => l.Name.Equals(request.TargetLayerName, StringComparison.OrdinalIgnoreCase));

                    if (targetLayer == null)
                        return new List<long>();

                    return targetLayer.GetSelection().GetObjectIDs().ToList();
                });

                if (objectIds.Count == 0)
                    return;

                // Lock the tool from accepting multiple selections during execution
                _isRunning = false;

                // Centralized creation date validation and supervisor authorization check
                var authResult = await FeatureDeletionGuardService.EvaluateAndAuthorizeByLayerAsync(
                    request.TargetLayerName,
                    objectIds,
                    maxDaysAllowed: 14);

                if (!authResult.IsAuthorized)
                    return;

                // Execute the deletion through your service with bypass enabled
                try
                {
                    DeletionController.IsAddInDeleting = true;
                    await PointFeatureDeleteService.DeleteSelectedAsync(request);
                }
                finally
                {
                    DeletionController.IsAddInDeleting = false;
                }

                // If supervisor override was active, archive snapshots to Documents\ArcGIS\Archive
                if (authResult.BypassUsed && authResult.GeoJsonSnapshots.Count > 0)
                {
                    try
                    {
                        FeatureDeletionGuardService.ArchiveFeatures(authResult.GeoJsonSnapshots);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Feature was deleted, but archival failed: {ex.Message}", "Archive Warning");
                    }
                }
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
    }
}