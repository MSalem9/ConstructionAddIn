using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using ConstructionAddIn.Helpers.PointHelpers.Valve1VentPecat;
using ConstructionAddIn.Helpers.PointHelpers;
using ConstructionAddIn.Tools.RemoveTools;
using System.Threading.Tasks;

namespace ConstructionAddIn.UserControls.Fittings.Valve1VentPecat
{
    /// <summary>
    /// ViewModel for the Valve-Pecat-(1)Vent feature UI.
    ///
    /// This ViewModel only connects the UI buttons to the shared generic
    /// draw/delete workflows.
    /// </summary>
    public class Valve1VentPecatViewModel : PropertyChangedBase
    {
        /// <summary>
        /// Command for drawing a Valve-Pecat-(1)Vent point feature.
        /// </summary>
        public RelayCommand DrawCommand { get; }

        /// <summary>
        /// Command for deleting a selected Valve-Pecat-(1)Vent point feature.
        /// </summary>
        public RelayCommand DeleteCommand { get; }

        public Valve1VentPecatViewModel()
        {
            //ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show("Valve1VentPecatViewModel loaded");
            DrawCommand = new RelayCommand(async () => await DrawAsync());
            DeleteCommand = new RelayCommand(async () => await DeleteAsync());
        }

        private async Task DrawAsync()
        {
            PointDrawContext.CurrentRequest = Valve1VentPecatDTOMapper.CreateRequest();

            await FrameworkApplication.SetCurrentToolAsync(
                "ConstructionAddIn_Tools_AddingTools_CreatePointFeatureTool");
        }

        private async Task DeleteAsync()
        {
            var request = Valve1VentPecatDTOMapper.CreateDeleteRequest();

            await RemovePointFeatureTool.StartAsync(request);
        }
    }
}