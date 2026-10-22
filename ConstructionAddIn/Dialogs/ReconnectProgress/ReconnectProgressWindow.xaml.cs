using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Controls;
using System.Windows;

namespace ConstructionAddIn.Dialogs.ReconnectProgress
{
    /// <summary>
    /// Non-modal window that shows each step of "Refresh Enterprise Connection" while it runs,
    /// and the result at the end.
    /// </summary>
    public partial class ReconnectProgressWindow : ProWindow
    {
        private readonly ReconnectProgressViewModel _viewModel;

        public ReconnectProgressWindow(ReconnectProgressViewModel viewModel)
        {
            InitializeComponent();

            _viewModel = viewModel;
            DataContext = viewModel;
            Owner = FrameworkApplication.Current.MainWindow;

            viewModel.IsWindowOpen = true;
            Closed += (s, e) => _viewModel.IsWindowOpen = false;
        }

        private void OnCloseClick(object sender, RoutedEventArgs e) => Close();
    }
}
