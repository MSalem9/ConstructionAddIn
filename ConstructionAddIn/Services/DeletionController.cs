using ArcGIS.Core.Events;
using ArcGIS.Desktop.Editing.Events;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Mapping;
using System;
using System.Collections.Generic;

namespace ConstructionAddIn.Services
{
    public static class DeletionController
    {
        private static readonly List<SubscriptionToken> _tokens = new List<SubscriptionToken>();
        private static bool _isDisplayingAlert = false;

        /// <summary>
        /// When true, allows deletion because it was executed by the custom add-in tool.
        /// </summary>
        public static bool IsAddInDeleting { get; set; } = false;

        public static async void InitializeProtection()
        {
            await QueuedTask.Run(() =>
            {
                Unsubscribe();

                var map = MapView.Active?.Map;
                if (map == null)
                    return;

                var featureLayers = map.GetLayersAsFlattenedList();
                foreach (var layer in featureLayers)
                {
                    if (layer is FeatureLayer fl)
                    {
                        var table = fl.GetTable();
                        if (table != null)
                        {
                            var token = RowDeletedEvent.Subscribe(OnRowDeleted, table);
                            _tokens.Add(token);
                        }
                    }
                }
            });
        }

        private static void OnRowDeleted(RowChangedEventArgs args)
        {
            // If the deletion was triggered by our Add-In, permit it
            if (IsAddInDeleting)
                return;

            // Set promptUser to FALSE to prevent the Yes/No retry loop
            args.CancelEdit("Standard delete is disabled. Use the custom Construction Add-In tools.", false);

            // Show a non-blocking toast notification in ArcGIS Pro
            NotifyUserOnce();
        }

        private static void NotifyUserOnce()
        {
            if (_isDisplayingAlert)
                return;

            _isDisplayingAlert = true;

            // Display standard ArcGIS Pro Toast Notification (avoids modal dialog freezes)
            Notification notification = new Notification
            {
                Title = "Deletion Disabled",
                Message = "Standard delete is disabled. Please use the Construction Add-In tools.",
                ImageUrl = @"pack://application:,,,/ArcGIS.Desktop.Resources;component/Images/GenericWarning32.png"
            };

            FrameworkApplication.AddNotification(notification);

            // Reset debounce flag after 1.5 seconds
            System.Threading.Tasks.Task.Delay(1500).ContinueWith(_ =>
            {
                _isDisplayingAlert = false;
            });
        }

        public static void Unsubscribe()
        {
            foreach (var token in _tokens)
            {
                RowDeletedEvent.Unsubscribe(token);
            }
            _tokens.Clear();
        }
    }
}