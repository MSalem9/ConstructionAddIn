using ConstructionAddIn.Helpers.LineHelpers;
using ConstructionAddIn.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConstructionAddIn.Helpers.PointHelpers.HotTap
{
    public class HotTapDTOMapper
    {
        private const string TargetLayerName = "HotTap";

        public static PointDrawRequest CreateRequest(CrossingHotTapDTO dto)
        {
            return new PointDrawRequest
            {
                TargetLayerName = TargetLayerName,
                KeepToolActiveAfterCreate = true,

                Attributes = new Dictionary<string, object>
                {
                    ["Name"] = dto.Name,
                    ["PipeName"] = dto.LineName,
                    ["ProjectName"] = dto.ProjectName,
                },
            };
        }

        public static PointDeleteRequest CreateDeleteRequest()
        {
            return new PointDeleteRequest
            {
                TargetLayerName = TargetLayerName,
                ConfirmationTitle = "Delete HotTap",
                ConfirmationMessage = "Are you sure you want to delete the selected HotTap?"
            };
        }
    }
}
