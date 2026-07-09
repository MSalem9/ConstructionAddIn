using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using ConstructionAddIn.Helpers.PointHelpers;
using ConstructionAddIn.Helpers.PointHelpers.BallValve1Vent;
using ConstructionAddIn.Tools.RemoveTools;
using System.Threading.Tasks;

namespace ConstructionAddIn.UserControls.Fittings.BallValve1Vent
{
    /// <summary>
    /// ViewModel for the Ball-Valve-(1)Vent feature UI.
    ///
    /// This ViewModel connects the UI buttons to the shared generic
    /// draw/delete workflows.
    /// </summary>
    public class BallValve1VentViewModel : PropertyChangedBase
    {
        public RelayCommand DrawCommand { get; }

        public RelayCommand DeleteCommand { get; }

        public BallValve1VentViewModel()
        {
            DrawCommand = new RelayCommand(async () => await DrawAsync());
            DeleteCommand = new RelayCommand(async () => await DeleteAsync());
        }

        private async Task DrawAsync()
        {
            PointDrawContext.CurrentRequest = BallValve1VentDTOMapper.CreateRequest();

            await FrameworkApplication.SetCurrentToolAsync(
                "ConstructionAddIn_Tools_AddingTools_CreatePointFeatureTool");
        }

        private async Task DeleteAsync()
        {
            var request = BallValve1VentDTOMapper.CreateDeleteRequest();

            await RemovePointFeatureTool.StartAsync(request);
        }
    }
}