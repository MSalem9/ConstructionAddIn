using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Framework.Dialogs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ConstructionAddIn.Services
{
    /// <summary>
    /// Builds the dynamic attributes for the Ball-Valve-(1)Vent construction fitting.
    ///
    /// Dynamic values come from the clicked/snapped pipe:
    /// - ANGLE
    /// - DIAMETER
    /// - Pipe_Name
    /// - ProjectName
    /// - stud Bolt2_size
    /// </summary>
    public static class BallValve1VentPlacementService
    {
        /// <summary>
        /// Builds dynamic attributes before creating the Ball-Valve-(1)Vent feature.
        /// Returning null cancels feature creation.
        /// </summary>
        public static async Task<Dictionary<string, object>> BuildAttributesAsync(MapPoint mapPoint)
        {
            var pipeInfo = await ConstructionPipeAttributeService.FindPipeAtPointAsync(mapPoint);

            if (pipeInfo == null)
            {
                MessageBox.Show(
                    "No pipe was found at the clicked location.",
                    "Ball-Valve-(1)Vent Placement");

                return null;
            }

            var diameter = ConstructionFittingRulesService.NormalizeDiameter(pipeInfo.Diameter);

            if (string.IsNullOrWhiteSpace(diameter))
            {
                MessageBox.Show(
                    "The selected pipe does not have a valid DIAMETER value.",
                    "Ball-Valve-(1)Vent Placement");

                return null;
            }

            var angle = ConstructionAngleService.GetArithmeticAngle(
                pipeInfo.Geometry,
                mapPoint);

            var studBolt2Size = ConstructionFittingRulesService.GetStudBoltSize120(diameter);

            return new Dictionary<string, object>
            {
                ["ANGLE"] = angle,
                ["DIAMETER"] = diameter,

                ["StudBolt2_Size"] = studBolt2Size,

                ["PipeName"] = pipeInfo.PipeName,
                ["ProjectName"] = pipeInfo.ProjectName
            };
        }
    }
}