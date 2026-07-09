using ConstructionAddIn.Services;
using System.Collections.Generic;

namespace ConstructionAddIn.Helpers.PointHelpers.Reducer
{
    public static class ReducerDTOMapper
    {
        private const string TargetLayerName = "reducer";

        public static PointDrawRequest CreateRequest()
        {
            return new PointDrawRequest
            {
                TargetLayerName = TargetLayerName,
                KeepToolActiveAfterCreate = true,

                Attributes = new Dictionary<string, object>
                {
                    ["CODE"] = null,
                    ["FUSION"] = null
                },

                BeforeCreateAttributes = ReducerPlacementService.BuildAttributesAsync,
                //ApplySymbologyAction = PointSymbologyService.ApplyReducerSymbol
            };
        }

        public static PointDeleteRequest CreateDeleteRequest()
        {
            return new PointDeleteRequest
            {
                TargetLayerName = TargetLayerName,
                ConfirmationTitle = "Delete Reducer",
                ConfirmationMessage = "Are you sure you want to delete the selected Reducer?"
            };
        }
    }
}








//using ConstructionAddIn.Services;
//using System.Collections.Generic;

//namespace ConstructionAddIn.Helpers.PointHelpers.Reducer
//{
//    public static class ReducerDTOMapper
//    {
//        private const string TargetLayerName = "Reducer";

//        public static PointDrawRequest CreateRequest()
//        {
//            return new PointDrawRequest
//            {
//                TargetLayerName = TargetLayerName,
//                KeepToolActiveAfterCreate = true,

//                Attributes = new Dictionary<string, object>
//                {
//                    ["code"] = null,
//                    ["fusion"] = null
//                },

//                BeforeCreateAttributes = ReducerPlacementService.BuildAttributesAsync,
//                ApplySymbologyAction = PointSymbologyService.ApplyReducerSymbol
//            };
//        }

//        public static PointDeleteRequest CreateDeleteRequest()
//        {
//            return new PointDeleteRequest
//            {
//                TargetLayerName = TargetLayerName,
//                ConfirmationTitle = "Delete Reducer",
//                ConfirmationMessage = "Are you sure you want to delete the selected Reducer?"
//            };
//        }
//    }
//}