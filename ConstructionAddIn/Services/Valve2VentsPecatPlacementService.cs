using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Framework.Dialogs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ConstructionAddIn.Services
{
    /// <summary>
    /// Builds the dynamic attributes for the Valve-Pecat-(2)Vent construction fitting.
    ///
    /// Dynamic values come from the clicked/snapped pipe:
    /// - ANGLE
    /// - DIAMETER
    /// - Pipe_Name
    /// - ProjectName
    /// - studBolt2_size
    /// - studBolt2_qty
    /// </summary>
    public static class Valve2VentsPecatPlacementService
    {
        /// <summary>
        /// Builds dynamic attributes before creating the Valve-Pecat-(2)Vent feature.
        /// Returning null cancels feature creation.
        /// </summary>
        public static async Task<Dictionary<string, object>> BuildAttributesAsync(MapPoint mapPoint)
        {
            var pipeInfo = await ConstructionPipeAttributeService.FindPipeAtPointAsync(mapPoint);

            if (pipeInfo == null)
            {
                MessageBox.Show(
                    "No pipe was found at the clicked location.",
                    "Valve-Pecat-(2)Vent Placement");

                return null;
            }

            var diameter = ConstructionFittingRulesService.NormalizeDiameter(pipeInfo.Diameter);

            if (string.IsNullOrWhiteSpace(diameter))
            {
                MessageBox.Show(
                    "The selected pipe does not have a valid DIAMETER value.",
                    "Valve-Pecat-(2)Vent Placement");

                return null;
            }

            var angle = ConstructionAngleService.GetArithmeticAngle(
                pipeInfo.Geometry,
                mapPoint);

            var studBolt2Size = ConstructionFittingRulesService.GetStudBoltSize120(diameter);
            var studBolt2Qty = ConstructionFittingRulesService.GetStudBoltQuantityByDiameter(diameter);

            return new Dictionary<string, object>
            {
                ["ANGLE"] = angle,
                ["DIAMETER"] = diameter,

                ["StudBolt2_size"] = studBolt2Size,
                ["StudBolt2_Qty"] = studBolt2Qty,

                ["PipeName"] = pipeInfo.PipeName,
                ["ProjectName"] = pipeInfo.ProjectName
            };
        }
    }
}