using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using ConstructionAddIn.Helpers.PointHelpers.ValvePecat;
using ConstructionAddIn.Helpers.PointHelpers;
using ConstructionAddIn.Tools.RemoveTools;
using System.Threading.Tasks;

namespace ConstructionAddIn.UserControls.Fittings.ValvePecat
{
    /// <summary>
    /// ViewModel for the Valve-Pecat feature UI.
    ///
    /// It only activates the shared generic draw/delete workflows.
    /// Feature-specific filling logic stays in:
    /// - ValvePecatDTOMapper
    /// - ValvePecatPlacementService
    /// </summary>
    public class ValvePecatViewModel : PropertyChangedBase
    {
        /// <summary>
        /// Command for drawing a Valve-Pecat point feature.
        /// </summary>
        public RelayCommand DrawCommand { get; }

        /// <summary>
        /// Command for deleting a selected Valve-Pecat point feature.
        /// </summary>
        public RelayCommand DeleteCommand { get; }

        public ValvePecatViewModel()
        {
            DrawCommand = new RelayCommand(async () => await DrawAsync());
            DeleteCommand = new RelayCommand(async () => await DeleteAsync());
        }

        private async Task DrawAsync()
        {
            PointDrawContext.CurrentRequest = ValvePecatDTOMapper.CreateRequest();

            await FrameworkApplication.SetCurrentToolAsync(
                "ConstructionAddIn_Tools_AddingTools_CreatePointFeatureTool");
        }

        private async Task DeleteAsync()
        {
            var request = ValvePecatDTOMapper.CreateDeleteRequest();

            await RemovePointFeatureTool.StartAsync(request);
        }
    }
}