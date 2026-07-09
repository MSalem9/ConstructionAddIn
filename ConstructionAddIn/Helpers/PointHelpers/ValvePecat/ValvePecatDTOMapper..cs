using ConstructionAddIn.Services;
using System.Collections.Generic;

namespace ConstructionAddIn.Helpers.PointHelpers.ValvePecat
{
    /// <summary>
    /// Converts Valve-Pecat DTO/UI data into generic point draw/delete requests.
    ///
    /// This mapper connects the Valve-Pecat feature to the shared generic
    /// point creation/deletion architecture.
    /// </summary>
    public static class ValvePecatDTOMapper
    {
        /// <summary>
        /// Target feature layer name in ArcGIS Pro.
        /// Change this value only if the actual layer name is different.
        /// </summary>
        private const string TargetLayerName = "hpValve_Pecat";

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

                    ["GateValve_Desc"] = "GATE VALVE,CAST STEEL , GIS V7 PART 1, R.F- FLG ENDED CL.150, ASME B16.5 NON RISING STEM, FALSE CAP(25*25)",
                    ["Gasket_Desc"] = "SPIRAL WOUND GASKET CL. 150 #ASME B16.20 ,  CS/SS",
                    ["Gasket_Qty"] = 2,

                    ["Pecat_Desc"] = "PECATSDR11 PE100-E.F-GIS -PL3, ASA 150-ASME B16.5",
                    ["Pecat_Qty"] = 2,

                    ["studBolt_Desc"] = "Stud Bolt  A193GR.B7,C/W 2NUT A194GR.2H AND 2 WASHER-ASME B1.1"
                },

                //BeforeCreateAttributes = ValvePecatPlacementService.BuildAttributesAsync,
                //ApplySymbologyAction = PointSymbologyService.ApplyValveRegulatorSymbol

                BeforeCreateAttributes = ValvePecatPlacementService.BuildAttributesAsync,
                //ApplySymbologyAction = PointSymbologyService.ApplyValvePecatSymbol
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
                ConfirmationTitle = "Delete Valve-Pecat",
                ConfirmationMessage = "Are you sure you want to delete the selected Valve-Pecat?"
            };
        }
    }
}