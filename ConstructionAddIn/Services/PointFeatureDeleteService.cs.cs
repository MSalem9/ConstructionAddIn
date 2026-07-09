using ArcGIS.Core.Data;
using ArcGIS.Desktop.Editing;
using ArcGIS.Desktop.Framework.Dialogs;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ConstructionAddIn.Helpers.PointHelpers;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace ConstructionAddIn.Services
{
    /// <summary>
    /// Generic service responsible for deleting one selected construction point feature.
    ///
    /// This service is layer-specific through PointDeleteRequest.TargetLayerName.
    /// It is reused by all construction fitting delete buttons.
    /// </summary>
    public static class PointFeatureDeleteService
    {
        /// <summary>
        /// Deletes exactly one selected feature from the target layer.
        ///
        /// Workflow:
        /// - find target layer
        /// - read selected OIDs
        /// - validate exactly one selected feature
        /// - ask confirmation
        /// - delete selected feature
        /// </summary>
        public static async Task<bool> DeleteSelectedAsync(PointDeleteRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.TargetLayerName))
                return false;

            return await QueuedTask.Run(() =>
            {
                var map = MapView.Active?.Map;
                if (map == null)
                    return false;

                var targetLayer = map.GetLayersAsFlattenedList()
                    .OfType<FeatureLayer>()
                    .FirstOrDefault(layer =>
                        layer.Name.Equals(request.TargetLayerName, StringComparison.OrdinalIgnoreCase));

                if (targetLayer == null)
                {
                    MessageBox.Show(request.LayerNotFoundMessage, request.ConfirmationTitle);
                    return false;
                }

                var selection = targetLayer.GetSelection();
                var selectedOids = selection.GetObjectIDs();

                if (selectedOids == null || selectedOids.Count == 0)
                {
                    MessageBox.Show(request.NothingSelectedMessage, request.ConfirmationTitle);
                    return false;
                }

                if (selectedOids.Count > 1)
                {
                    MessageBox.Show(request.MoreThanOneSelectedMessage, request.ConfirmationTitle);
                    return false;
                }

                var result = MessageBox.Show(
                    request.ConfirmationMessage,
                    request.ConfirmationTitle,
                    System.Windows.MessageBoxButton.YesNo,
                    System.Windows.MessageBoxImage.Warning);

                if (result != System.Windows.MessageBoxResult.Yes)
                    return false;

                var editOperation = new EditOperation
                {
                    Name = $"Delete {request.TargetLayerName}"
                };

                editOperation.Delete(targetLayer, selectedOids);

                return editOperation.Execute();
            });
        }
    }
}