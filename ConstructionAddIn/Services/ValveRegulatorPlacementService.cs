using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Framework.Dialogs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ConstructionAddIn.Services
{
    /// <summary>
    /// Builds the dynamic attributes for Valve-Regulator placement.
    ///
    /// Responsibilities:
    /// - find pipe under clicked point
    /// - read pipe DIAMETER
    /// - read Pipe Name
    /// - read ProjectName
    /// - calculate geographic ANGLE
    /// - apply diameter-based size/quantity rules
    /// </summary>
    public static class ValveRegulatorPlacementService
    {
        /// <summary>
        /// Builds dynamic attributes before creating the feature.
        ///
        /// Return null to cancel creation.
        /// </summary>
        public static async Task<Dictionary<string, object>> BuildAttributesAsync(MapPoint mapPoint)
        {
            var pipeInfo = await ConstructionPipeAttributeService.FindPipeAtPointAsync(mapPoint);

            if (pipeInfo == null)
            {
                MessageBox.Show(
                    "No pipe was found at the clicked location.",
                    "Valve-Regulator Placement");

                return null;
            }

            var diameter = ConstructionFittingRulesService.NormalizeDiameter(pipeInfo.Diameter);

            if (string.IsNullOrWhiteSpace(diameter))
            {
                MessageBox.Show(
                    "The selected pipe does not have a valid DIAMETER value.",
                    "Valve-Regulator Placement");

                return null;
            }

            var angle = ConstructionAngleService.GetArithmeticAngle(pipeInfo.Geometry, mapPoint);

            var studBoltSize = ConstructionFittingRulesService.GetStudBoltSize110(diameter);
            var studBoltQty = ConstructionFittingRulesService.GetStudBoltQuantityByDiameter(diameter);

            return new Dictionary<string, object>
            {
                ["ANGLE"] = angle,
                ["DIAMETER"] = diameter,

                ["GateValve_Size"] = diameter,
                ["SpiralGasket_Size"] = diameter,
                ["BlindFlange_Size"] = diameter,

                ["StudBolt_Size"] = studBoltSize,
                ["StudBolt_Qty"] = studBoltQty,

                ["PipeName"] = pipeInfo.PipeName,
                ["ProjectName"] = pipeInfo.ProjectName
            };
        }
    }
}