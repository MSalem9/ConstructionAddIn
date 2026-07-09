using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using ConstructionAddIn.Helpers.PointHelpers;
using ConstructionAddIn.Helpers.PointHelpers.Reducer;
using ConstructionAddIn.Tools.RemoveTools;
using System.Threading.Tasks;

namespace ConstructionAddIn.UserControls.Fittings.Reducer
{
    public class ReducerViewModel : PropertyChangedBase
    {
        public RelayCommand DrawCommand { get; }
        public RelayCommand DeleteCommand { get; }

        public ReducerViewModel()
        {
            DrawCommand = new RelayCommand(async () => await DrawAsync());
            DeleteCommand = new RelayCommand(async () => await DeleteAsync());
        }

        private async Task DrawAsync()
        {
            PointDrawContext.CurrentRequest = ReducerDTOMapper.CreateRequest();

            await FrameworkApplication.SetCurrentToolAsync(
                "ConstructionAddIn_Tools_AddingTools_CreatePointFeatureTool");
        }

        private async Task DeleteAsync()
        {
            var request = ReducerDTOMapper.CreateDeleteRequest();

            await RemovePointFeatureTool.StartAsync(request);
        }
    }
}