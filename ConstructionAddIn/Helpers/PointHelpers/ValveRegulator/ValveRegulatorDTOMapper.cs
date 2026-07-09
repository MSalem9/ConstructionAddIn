using ConstructionAddIn.Services;
using System.Collections.Generic;

namespace ConstructionAddIn.Helpers.PointHelpers.ValveRegulator
{
    /// <summary>
    /// Converts Valve-Regulator DTO/UI data into generic point draw/delete requests.
    /// </summary>
    public static class ValveRegulatorDTOMapper
    {
        private const string TargetLayerName = "hpValve_Pecat_Reg";

        public static PointDrawRequest CreateRequest()
        {
            return new PointDrawRequest
            {
                TargetLayerName = TargetLayerName,
                KeepToolActiveAfterCreate = true,

                Attributes = new Dictionary<string, object>
                {
                    ["TYPE"] = "Gate Valve",
                    ["GateValve_Desc"] = "GATE VALVE,CAST STEEL , GIS V7 PART 1, R.F- FLG ENDED CL.150, ASME B16.5 NON RISING STEM, FALSE CAP(25*25)",
                    ["SpiralGasket_Desc"] = "SPIRAL WOUND GASKET CL. 150 #ASME B16.20 ,  CS/SS",
                    ["SpiralGasket_Qty"] = 2,
                    ["BlindFlange_Desc"] = "BLIND R.F  FLANGE ASME B16.5 ASTM A105",
                    ["Pecat_Desc"] = "PECATSDR11 PE100-E.F-GIS -PL3, ASA 150-ASME B16.5",
                    ["Pecat_Size"] = "90mm",
                    ["StudBolt_Desc"] = "Stud Bolt  A193GR.B7,C/W 2NUT A194GR.2H AND 2 WASHER-ASME B1.1"
                },

                BeforeCreateAttributes = ValveRegulatorPlacementService.BuildAttributesAsync,
                //ApplySymbologyAction = PointSymbologyService.ApplyValveRegulatorSymbol
            };
        }

        public static PointDeleteRequest CreateDeleteRequest()
        {
            return new PointDeleteRequest
            {
                TargetLayerName = TargetLayerName,
                ConfirmationTitle = "Delete Valve-Regulator",
                ConfirmationMessage = "Are you sure you want to delete the selected Valve-Regulator?"
            };
        }
    }
}