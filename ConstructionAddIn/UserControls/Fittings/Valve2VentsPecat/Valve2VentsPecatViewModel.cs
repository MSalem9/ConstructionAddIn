using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using ConstructionAddIn.Helpers.PointHelpers;
using ConstructionAddIn.Helpers.PointHelpers.Valve2VentsPecat;
using ConstructionAddIn.Tools.RemoveTools;
using System.Threading.Tasks;

namespace ConstructionAddIn.UserControls.Fittings.Valve2VentsPecat
{
    /// <summary>
    /// ViewModel for the Valve-Pecat-(2)Vent feature UI.
    ///
    /// This ViewModel connects the UI buttons to the shared generic
    /// draw/delete workflows.
    /// </summary>
    public class Valve2VentsPecatViewModel : PropertyChangedBase
    {
        public RelayCommand DrawCommand { get; }

        public RelayCommand DeleteCommand { get; }

        public Valve2VentsPecatViewModel()
        {
            DrawCommand = new RelayCommand(async () => await DrawAsync());
            DeleteCommand = new RelayCommand(async () => await DeleteAsync());
        }

        private async Task DrawAsync()
        {
            PointDrawContext.CurrentRequest = Valve2VentsPecatDTOMapper.CreateRequest();

            await FrameworkApplication.SetCurrentToolAsync(
                "ConstructionAddIn_Tools_AddingTools_CreatePointFeatureTool");
        }

        private async Task DeleteAsync()
        {
            var request = Valve2VentsPecatDTOMapper.CreateDeleteRequest();

            await RemovePointFeatureTool.StartAsync(request);
        }
    }
}