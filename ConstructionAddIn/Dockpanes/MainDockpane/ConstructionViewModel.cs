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
using ConstructionAddIn.Services.EnterpriseConnection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ConstructionAddIn.Dockpanes.MainDockpane
{
    internal class ConstructionViewModel : DockPane
    {
        private const string _dockPaneID = "ConstructionAddIn_Dockpanes_MainDockpane_Construction";

        protected ConstructionViewModel()
        {
            ReconnectCommand = new ArcGIS.Desktop.Framework.RelayCommand(
                async () => await EnterpriseConnectionService.ReconnectAsync());

            EnterpriseConnectionMonitor.StatusChanged += OnConnectionStatusChanged;
        }

        #region Enterprise connection

        /// <summary>False while the enterprise connection is lost: the whole form is disabled.</summary>
        public bool IsEditingAllowed => EnterpriseConnectionMonitor.IsEditingAllowed;

        /// <summary>Drives the red "connection lost" banner.</summary>
        public bool IsConnectionLost => !EnterpriseConnectionMonitor.IsEditingAllowed;

        public string ConnectionMessage =>
            (EnterpriseConnectionMonitor.BlockReason ?? "Enterprise database connection lost.") +
            " Editing is disabled until you reconnect.";

        public ICommand ReconnectCommand { get; }

        private void OnConnectionStatusChanged()
        {
            System.Windows.Application.Current?.Dispatcher.BeginInvoke(() =>
            {
                NotifyPropertyChanged(nameof(IsEditingAllowed));
                NotifyPropertyChanged(nameof(IsConnectionLost));
                NotifyPropertyChanged(nameof(ConnectionMessage));
            });
        }

        #endregion

        /// <summary>
        /// Show the DockPane.
        /// </summary>
        internal static void Show()
        {
            DockPane pane = FrameworkApplication.DockPaneManager.Find(_dockPaneID);
            if (pane == null)
                return;

            pane.Activate();
        }

        /// <summary>
        /// Text shown near the top of the DockPane.
        /// </summary>
        private string _heading = "My DockPane";
        public string Heading
        {
            get => _heading;
            set => SetProperty(ref _heading, value);
        }
    }

    /// <summary>
    /// Button implementation to show the DockPane.
    /// </summary>
    internal class Construction_ShowButton : Button
    {
        protected override void OnClick()
        {
            ConstructionViewModel.Show();
        }
    }
}
