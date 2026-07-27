using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Mapping;
using ConstructionAddIn.Helpers.PointHelpers;
using ConstructionAddIn.Services;
using System.Threading.Tasks;

namespace ConstructionAddIn.Tools.AddingTools
{
    /// <summary>
    /// Generic construction point creation map tool.
    ///
    /// This tool does not know which fitting it is creating.
    /// It reads PointDrawContext.CurrentRequest, then delegates feature creation
    /// to PointFeatureCreationService.
    ///
    /// All fitting-specific logic should stay in:
    /// - DTOMapper
    /// - PlacementService
    /// - PointSymbologyService
    /// </summary>
    public class CreatePointFeatureTool : MapTool
    {
        /// <summary>
        /// Initializes the tool as a point sketch tool.
        /// The user clicks once on the map to place the fitting.
        /// </summary>
        /// 

        public CreatePointFeatureTool()
        {
            IsSketchTool = true;
            SketchType = SketchGeometryType.Point;
            SketchOutputMode = SketchOutputMode.Map;

            UseSnapping = true;
        }

        //public CreatePointFeatureTool()
        //{
        //    IsSketchTool = true;
        //    SketchType = SketchGeometryType.Point;
        //    SketchOutputMode = SketchOutputMode.Map;
        //}
        //public CreatePointFeatureTool()
        //{
        //    ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show("CreatePointFeatureTool constructor reached");

        //    IsSketchTool = true;
        //    SketchType = SketchGeometryType.Point;
        //    SketchOutputMode = SketchOutputMode.Map;
        //}

        /// <summary>
        /// Runs when the user clicks on the map.
        /// The clicked geometry is expected to be a MapPoint.
        /// </summary>
        protected override async Task<bool> OnSketchCompleteAsync(Geometry geometry)
        {
            if (geometry is not MapPoint mapPoint)
                return false;

            var request = PointDrawContext.CurrentRequest;

            if (request == null)
                return false;

            bool created = await PointFeatureCreationService.CreateAsync(mapPoint, request);

            if (!request.KeepToolActiveAfterCreate)
                PointDrawContext.CurrentRequest = null;

            _ = FrameworkApplication.SetCurrentToolAsync("esri_mapping_exploreTool");

            return created;
        }
    }
}