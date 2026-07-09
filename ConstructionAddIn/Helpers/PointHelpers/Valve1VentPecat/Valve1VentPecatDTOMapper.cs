using ConstructionAddIn.Helpers.PointHelpers;
using ConstructionAddIn.Services;
using System.Collections.Generic;

namespace ConstructionAddIn.Helpers.PointHelpers.Valve1VentPecat
{
    /// <summary>
    /// Converts Valve-Pecat-(1)Vent DTO/UI data into generic point draw/delete requests.
    ///
    /// This mapper connects the Valve-Pecat-(1)Vent feature to the shared generic
    /// point creation/deletion architecture.
    /// </summary>
    public static class Valve1VentPecatDTOMapper
    {
        /// <summary>
        /// Target feature layer name in ArcGIS Pro.
        /// Change this only if the actual layer name is different.
        /// </summary>
        private const string TargetLayerName = "hpValve_Pecat_1Vent";

        /// <summary>
        /// Creates the draw request used by the generic CreatePointFeatureTool.
        /// </summary>
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
                    ["Gasket1_Desc"] = "SPIRAL WOUND GASKET CL. 150 #ASME B16.20 ,  CS/SS",
                    ["Gasket2_Desc"] = "SPIRAL WOUND GASKET CL. 150 #ASME B16.20",
                    ["Gasket1_Qty"] = 2,
                    ["Gasket2_Qty"] = 4,
                    ["Gasket2_Size"] = "1\"",
                    

                    ["Pecat_Desc"] = "PECATSDR11 PE100-E.F-GIS -PL3, ASA 150-ASME B16.5 W/Vent",
                    ["BlindFlange1_Desc"] = "BLIND R.F  FLANGE ASME B16.5 ASTM A105",

                    ["Pipe_Desc"] = "PIPE API 5L, GR B ,ERW  SCH.40 , PSL 2- P.E COATED",
                    ["Pipe_qty"] = 0.5,
                    ["Pipe_unit"] = "Meter",

                    ["BallValve_size"] = "1\"",
                    ["BallValve_Desc"] = "FULL BORE BALL VALVE- FC(25*25), API 6D- R.F -FLG ENDED - ASME B16.5",
                    ["Ballvalve_qty"] = 2,

                    ["SliponFlange_size"] = "1\"",
                    ["SliponFlange_Desc"] = "Slip-ON R.F  FLANGE  ASME B16.5 ASTM A105",
                    ["SliponFlange_qty"] = 2,

                    ["BlindFlange2_size"] = "1\"",
                    ["BlindFlange2_Desc"] = "BLIND R.F  FLANGE ASME B16.5 ASTM A105",

                    ["StudBolt1_Desc"] = "Stud Bolt  A193GR.B7,C/W 2NUT A194GR.2H AND 2 WASHER-ASME B1.1",
                    ["StudBolt1_Size"] = "1/2\" x 70 mm",
                    ["StudBolt1_Qty"] = 16,

                    ["studBolt2_Desc"] = "Stud Bolt  A193GR.B7,C/W 2NUT A194GR.2H AND 2 WASHER-ASME B1.1"
                },

                BeforeCreateAttributes = Valve1VentPecatPlacementService.BuildAttributesAsync,
                //ApplySymbologyAction = PointSymbologyService.ApplyValve1VentPecatSymbol
            };
        }

        /// <summary>
        /// Creates the delete request used by the generic RemovePointFeatureTool.
        /// </summary>
        public static PointDeleteRequest CreateDeleteRequest()
        {
            return new PointDeleteRequest
            {
                TargetLayerName = TargetLayerName,
                ConfirmationTitle = "Delete Valve-Pecat-(1)Vent",
                ConfirmationMessage = "Are you sure you want to delete the selected Valve-Pecat-(1)Vent?"
            };
        }
    }
}