using ConstructionAddIn.Helpers.PointHelpers;
using ConstructionAddIn.Services;
using System.Collections.Generic;

namespace ConstructionAddIn.Helpers.PointHelpers.Valve2VentsPecat
{
    /// <summary>
    /// Converts Valve-Pecat-(2)Vent data into generic point draw/delete requests.
    /// </summary>
    public static class Valve2VentsPecatDTOMapper
    {
        private const string TargetLayerName = "hpValve_Pecat_2Vent";

        public static PointDrawRequest CreateRequest()
        {
            return new PointDrawRequest
            {
                TargetLayerName = TargetLayerName,
                KeepToolActiveAfterCreate = true,

                Attributes = new Dictionary<string, object>
                {
                    ["TYPE"] = "Gate Valve",

                    ["GateValve_Desc"] = "GATE VALVE,DI , GIS V7 PART 1, R.F- FLG ENDED CL.150, ASME B16.5 NON RISING STEM, FALSE CAP(25*25)",
                    ["PipeGasket_Desc"] = "SPIRAL WOUND GASKET CL. 150 #ASME B16.20 ,  CS/SS",
                    ["PipeGasket_Qty"] = 2,

                    ["Pecat_Desc"] = "PECAT SDR11 PE100-E.F-GIS -PL3, ASA 150-ASME B16.5 W/Vent",
                    ["Pecat_Qty"] = 2,

                    ["Pipe_Desc"] = "PIPE API 5L, GR B ,ERW  SCH.40 , PSL 2- P.E COATED",

                    ["BallValve_Desc"] = "FULL BORE BALL VALVE- FC(25*25), API 6D- R.F -FLG ENDED - ASME B16.5",
                    ["BallValve_size"] = "1\"",

                    ["SlipOnFlange_Desc"] = "Slip-ON R.F  FLANGE  ASME B16.5 ASTM A105",
                    ["SlipOnFlange_Qty"] = 4,
                    ["SlipOnFlange_size"] = "1\"",

                    ["BlindFlange_Desc"] = "BLIND R.F  FLANGE ASME B16.5 ASTM A105",
                    ["BlindFlange_Qty"] = 2,
                    ["BlindFlange_size"] = "1\"",

                    ["VentGasket_Desc"] = "SPIRAL WOUND GASKET CL. 150 #ASME B16.20",
                    ["VentSpiralGasket_Qty"] = 8,
                    ["VentspiralGasket_size"] = "1\"",

                    ["Pipe_size"] = "1\"",

                    ["StudBolt1_Desc"] = "Stud Bolt  A193GR.B7,C/W 2NUT A194GR.2H AND 2 WASHER-ASME B1.1",
                    ["StudBolt1_size"] = "1/2\" x 70 mm",
                    ["StudBolt_Qty"] = 32,

                    ["StudBolt2_Desc"] = "Stud Bolt  A193GR.B7,C/W 2NUT A194GR.2H AND 2 WASHER-ASME B1.1"
                },

                BeforeCreateAttributes = Valve2VentsPecatPlacementService.BuildAttributesAsync,
                //ApplySymbologyAction = PointSymbologyService.ApplyValve2VentsPecatSymbol
            };
        }

        public static PointDeleteRequest CreateDeleteRequest()
        {
            return new PointDeleteRequest
            {
                TargetLayerName = TargetLayerName,
                ConfirmationTitle = "Delete Valve-Pecat-(2)Vent",
                ConfirmationMessage = "Are you sure you want to delete the selected Valve-Pecat-(2)Vent?"
            };
        }
    }
}