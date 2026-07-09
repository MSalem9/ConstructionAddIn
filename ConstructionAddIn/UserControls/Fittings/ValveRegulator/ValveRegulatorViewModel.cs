using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using ConstructionAddIn.Helpers.PointHelpers;
using ConstructionAddIn.Helpers.PointHelpers.ValveRegulator;
using ConstructionAddIn.Tools.RemoveTools;
using System.Threading.Tasks;

namespace ConstructionAddIn.UserControls.Fittings.ValveRegulator
{
    /// <summary>
    /// ViewModel for the Valve-Regulator feature UI.
    ///
    /// It activates the shared generic creation/deletion workflow.
    /// </summary>
    public class ValveRegulatorViewModel : PropertyChangedBase
    {
        public RelayCommand DrawCommand { get; }
        public RelayCommand DeleteCommand { get; }

        public ValveRegulatorViewModel()
        {
            DrawCommand = new RelayCommand(async () => await DrawAsync());
            DeleteCommand = new RelayCommand(async () => await DeleteAsync());
        }

        /// <summary>
        /// Creates a Valve-Regulator draw request and activates the generic point creation tool.
        /// </summary>
        private async Task DrawAsync()
        {
            PointDrawContext.CurrentRequest = ValveRegulatorDTOMapper.CreateRequest();

            await FrameworkApplication.SetCurrentToolAsync(
                "ConstructionAddIn_Tools_AddingTools_CreatePointFeatureTool");
        }
        //private async Task DrawAsync()
        //{
        //    ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show("Draw command triggered");

        //    PointDrawContext.CurrentRequest = ValveRegulatorDTOMapper.CreateRequest();

        //    await FrameworkApplication.SetCurrentToolAsync(
        //        "ConstructionAddIn_Tools_AddingTools_CreatePointFeatureTool");
        //}
        //private async Task DrawAsync()
        //{
        //    ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show("Draw command triggered");

        //    PointDrawContext.CurrentRequest = ValveRegulatorDTOMapper.CreateRequest();

        //    await FrameworkApplication.SetCurrentToolAsync(
        //        "ConstructionAddIn_Tools_AddingTools_CreatePointFeatureTool");

        //    ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show("SetCurrentToolAsync finished");
        //}

        /// <summary>
        /// Starts the generic delete workflow for the Valve-Regulator layer.
        /// </summary>
        private async Task DeleteAsync()
        {
            var request = ValveRegulatorDTOMapper.CreateDeleteRequest();

            await RemovePointFeatureTool.StartAsync(request);
        }
    }
}