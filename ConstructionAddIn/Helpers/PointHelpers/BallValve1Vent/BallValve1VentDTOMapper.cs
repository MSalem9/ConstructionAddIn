using ConstructionAddIn.Helpers.PointHelpers;
using ConstructionAddIn.Services;
using System.Collections.Generic;
using System.Diagnostics.Metrics;

namespace ConstructionAddIn.Helpers.PointHelpers.BallValve1Vent
{
    /// <summary>
    /// Converts Ball-Valve-(1)Vent DTO/UI data into generic point draw/delete requests.
    /// </summary>
    public static class BallValve1VentDTOMapper
    {
        private const string TargetLayerName = "hpBallValve_Pecat_1Vent";

        public static PointDrawRequest CreateRequest()
        {
            return new PointDrawRequest
            {
                TargetLayerName = TargetLayerName,
                KeepToolActiveAfterCreate = true,

                Attributes = new Dictionary<string, object>
                {
                    ["TYPE"] = "Ball Valve",

                    ["FlangedEnd_Desc"] = "FULL BORE FLANGED END  BALL VALVE  CL.150 # R.F, F/CAP ,CS ,API 6D",
                    ["PipeSpiralGasket_Desc"] = "SPIRAL WOUND GASKET CL. 150 #ASME B16.20 ,  CS/SS",
                    ["PipeSpiralGasket_Qty"] = 2,

                    ["Pecat_Desc"] = "PECATSDR11 PE100-E.F-GIS -PL3, ASA 150-ASME B16.5 W/Vent",
                    ["W_N_R_F_Flange_Desc"] = "W.N R.F  FLANGE CL. 150  # ASME B16.5, ASTM A105 ,SCH.40",

                    ["Pipe_Desc"] = "PIPE API 5L, GR B ,ERW  SCH.40 , PSL 2- P.E COATED",
                    ["Pipe_size"] = "1\"",
                    ["Pipe_qty"] = 0.5,
                    ["Pipe_Unit"] = "Meter",

                    ["BallValveLever_Size"] = "1\"",
                    ["BallValveLever_Qty"] = 2,
                    ["BallValveLever_Desc"] = "FULL BORE BALL VALVE- FC(25*25), API 6D- R.F -FLG ENDED - ASME B16.5",

                    ["SlipOnFlange_Desc"] = "Slip-ON R.F  FLANGE  ASME B16.5 ASTM A105",
                    ["SliponFlange_size"] = "1\"",
                    ["SliponFlange_qty"] = 2,

                    ["BlindFlange_Desc"] = "BLIND R.F  FLANGE ASME B16.5 ASTM A105",
                    ["BlindFlange_size"] = "1\"",

                    ["VentspiralGasket_size"] = "1\"",
                    ["VentspiralGasket_qty"] = 4,
                    ["VentspiralGasket_Desc"] = "SPIRAL WOUND GASKET CL. 150 #ASME B16.20",

                    ["StudBolt1_Desc"] = "Stud Bolt  A193GR.B7,C/W 2NUT A194GR.2H AND 2 WASHER-ASME B1.1",
                    ["studBolt1_size"] = "1/2\" x 70 mm",
                    ["StudBolt1_Qty"] = 16,

                    ["studBolt2_Desc"] = "Stud Bolt  A193GR.B7,C/W 2NUT A194GR.2H AND 2 WASHER-ASME B1.1",
                    ["studBolt2_qty"] = 16

                },

                BeforeCreateAttributes = BallValve1VentPlacementService.BuildAttributesAsync,
                //ApplySymbologyAction = PointSymbologyService.ApplyBallValve1VentSymbol
            };
        }

        public static PointDeleteRequest CreateDeleteRequest()
        {
            return new PointDeleteRequest
            {
                TargetLayerName = TargetLayerName,
                ConfirmationTitle = "Delete Ball-Valve-(1)Vent",
                ConfirmationMessage = "Are you sure you want to delete the selected Ball-Valve-(1)Vent?"
            };
        }
    }
}