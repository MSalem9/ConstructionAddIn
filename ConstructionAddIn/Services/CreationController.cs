using ArcGIS.Core.Events;
using ArcGIS.Desktop.Editing.Events;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ConstructionAddIn.Services.EnterpriseConnection;
using System;
using System.Collections.Generic;

namespace ConstructionAddIn.Services
{
    public static class CreationController
    {
        private static readonly List<SubscriptionToken> _tokens = new List<SubscriptionToken>();
        private static bool _isDisplayingAlert = false;

        /// <summary>
        /// When true, allows row creation because it was initiated by a custom Add-In tool.
        /// When false, native ArcGIS Pro creation operations are intercepted and canceled.
        /// </summary>
        public static bool IsAddInCreating { get; set; } = false;

        /// <summary>
        /// Subscribes to RowCreatedEvent across all feature layers in the active map.
        /// </summary>
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
                            var token = RowCreatedEvent.Subscribe(OnRowCreated, table);
                            _tokens.Add(token);
                        }
                    }
                }
            });
        }

        private static void OnRowCreated(RowChangedEventArgs args)
        {
            // While the enterprise connection is lost, nothing may be created, not even by the Add-In
            if (!EnterpriseConnectionMonitor.IsEditingAllowed)
            {
                args.CancelEdit("Editing is disabled because the enterprise database connection was lost. Use Refresh Enterprise Connection.", false);
                return;
            }

            // If the creation was triggered by our Add-In, allow it
            if (IsAddInCreating)
                return;

            // Silently cancel and roll back the edit without triggering retry loops
            args.CancelEdit("Standard feature creation is disabled. Please use the Construction Add-In tools.", false);

            NotifyUserOnce();
        }

        private static void NotifyUserOnce()
        {
            if (_isDisplayingAlert)
                return;

            _isDisplayingAlert = true;

            Notification notification = new Notification
            {
                Title = "Feature Creation Blocked",
                Message = "Standard creation is disabled. Please use the Construction Add-In tools.",
                ImageUrl = @"pack://application:,,,/ArcGIS.Desktop.Resources;component/Images/GenericWarning32.png"
            };

            FrameworkApplication.AddNotification(notification);

            System.Threading.Tasks.Task.Delay(1500).ContinueWith(_ =>
            {
                _isDisplayingAlert = false;
            });
        }

        public static void Unsubscribe()
        {
            foreach (var token in _tokens)
            {
                RowCreatedEvent.Unsubscribe(token);
            }
            _tokens.Clear();
        }
    }
}