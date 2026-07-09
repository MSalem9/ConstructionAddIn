using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Framework.Dialogs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ConstructionAddIn.Services
{
    /// <summary>
    /// Builds the dynamic attributes for the Valve-Pecat construction fitting.
    ///
    /// This service is called by ValvePecatDTOMapper through:
    /// PointDrawRequest.BeforeCreateAttributes.
    ///
    /// Responsibilities:
    /// - Find the snapped/intersected pipe.
    /// - Read pipe DIAMETER.
    /// - Read pipe Name into Pipe Name.
    /// - Read pipe ProjectName.
    /// - Calculate geographic ANGLE.
    /// - Fill diameter-based stud bolt size and quantity.
    /// </summary>
    public static class ValvePecatPlacementService
    {
        /// <summary>
        /// Builds dynamic attributes before creating the Valve-Pecat feature.
        ///
        /// Returning null cancels creation.
        /// </summary>
        public static async Task<Dictionary<string, object>> BuildAttributesAsync(MapPoint mapPoint)
        {
            var pipeInfo = await ConstructionPipeAttributeService.FindPipeAtPointAsync(mapPoint);

            if (pipeInfo == null)
            {
                MessageBox.Show(
                    "No pipe was found at the clicked location.",
                    "Valve-Pecat Placement");

                return null;
            }

            var diameter = ConstructionFittingRulesService.NormalizeDiameter(pipeInfo.Diameter);

            if (string.IsNullOrWhiteSpace(diameter))
            {
                MessageBox.Show(
                    "The selected pipe does not have a valid DIAMETER value.",
                    "Valve-Pecat Placement");

                return null;
            }

            var angle = ConstructionAngleService.GetArithmeticAngle(
                pipeInfo.Geometry,
                mapPoint);

            var studBoltSize = ConstructionFittingRulesService.GetStudBoltSize120(diameter);
            var studBoltQty = ConstructionFittingRulesService.GetStudBoltQuantityByDiameter(diameter);

            return new Dictionary<string, object>
            {
                ["ANGLE"] = angle,
                ["DIAMETER"] = diameter,

                ["StudBolt_Size"] = studBoltSize,
                ["StudBolt_Qty"] = studBoltQty,

                ["PipeName"] = pipeInfo.PipeName,
                ["ProjectName"] = pipeInfo.ProjectName
            };
        }
    }
}