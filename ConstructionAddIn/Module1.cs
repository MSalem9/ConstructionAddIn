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

        /// <summary>
        /// Retrieve the singleton instance to this module here
        /// </summary>
        public static Module1 Current => _this ??= (Module1)FrameworkApplication.FindModule("ConstructionAddIn_Module");

        #region Overrides

        /// <summary>
        /// Called by Framework when the module loads (ArcGIS Pro startup).
        /// </summary>
        protected override bool Initialize()
        {
            // Initial binding if a map view is already loaded
            if (MapView.Active != null)
            {
                DeletionController.InitializeProtection();
            }

            // Re-bind when switching between map tabs or opening a new map
            ActiveMapViewChangedEvent.Subscribe(args =>
            {
                if (args.IncomingView != null)
                {
                    DeletionController.InitializeProtection();
                }
            });

            // Re-bind when layers are added or removed dynamically
            LayersAddedEvent.Subscribe(args =>
            {
                DeletionController.InitializeProtection();
            });

            LayersRemovedEvent.Subscribe(args =>
            {
                DeletionController.InitializeProtection();
            });

            return base.Initialize();
        }

        /// <summary>
        /// Called by Framework when ArcGIS Pro is closing.
        /// </summary>
        /// <returns>False to prevent Pro from closing, otherwise True</returns>
        protected override bool CanUnload()
        {
            DeletionController.Unsubscribe();
            return true;
        }

        #endregion Overrides
    }
}