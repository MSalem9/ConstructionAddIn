using ArcGIS.Core.CIM;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Catalog;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Editing;
using ArcGIS.Desktop.Extensions;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Framework.Dialogs;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.KnowledgeGraph;
using ArcGIS.Desktop.Layouts;
using ArcGIS.Desktop.Mapping;
using ArcGIS.Desktop.Mapping.Events;
using ConstructionAddIn.Services;
using ConstructionAddIn.Services.EnterpriseConnection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ConstructionAddIn
{
    internal class Module1 : Module
    {
        private static Module1 _this = null;

        public static Module1 Current => _this ??= (Module1)FrameworkApplication.FindModule("ConstructionAddIn_Module");

        #region Overrides

        protected override bool Initialize()
        {
            // Initial binding if a map view is already active
            if (MapView.Active != null)
            {
                DeletionController.InitializeProtection();
                CreationController.InitializeProtection();
            }

            // Re-bind when switching maps or opening a new map view
            ActiveMapViewChangedEvent.Subscribe(args =>
            {
                if (args.IncomingView != null)
                {
                    DeletionController.InitializeProtection();
                    CreationController.InitializeProtection();
                }
            });

            // Re-bind when layers are added dynamically
            LayersAddedEvent.Subscribe(args =>
            {
                DeletionController.InitializeProtection();
                CreationController.InitializeProtection();
            });

            // Re-bind when layers are removed
            LayersRemovedEvent.Subscribe(args =>
            {
                DeletionController.InitializeProtection();
                CreationController.InitializeProtection();
            });

            // Watch the enterprise database connection; disables editing when it is lost
            EnterpriseConnectionMonitor.Start();

            return base.Initialize();
        }

        protected override bool CanUnload()
        {
            DeletionController.Unsubscribe();
            CreationController.Unsubscribe();
            EnterpriseConnectionMonitor.Stop();
            return true;
        }

        #endregion Overrides
    }
}