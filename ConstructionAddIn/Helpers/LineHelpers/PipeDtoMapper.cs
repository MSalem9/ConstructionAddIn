using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConstructionAddIn.Helpers.LineHelpers
{
    public static class PipeDtoMapper
    {
        public static LineDrawRequest ToPipeRequest(PipeDTO dto)
        {
            return new LineDrawRequest
            {
                TargetLineLayerName = "pipes",

                Attributes = new Dictionary<string, object>
                {
                    ["Name"] = dto.LineName,
                    ["ProjectName"] = dto.ProjectName,
                    ["Diameter"] = dto.PipeWidth,
                    ["PipeMatl"] = dto.PipeMatrial,
                    ["Details"] = dto.OptionalDetails
                }
            };
        }
        public static LineDrawRequest ToCrossingRequest(CrossingHotTapDTO dto)
        {
            return new LineDrawRequest
            {
                TargetLineLayerName = "CrossingNew",

                Attributes = new Dictionary<string, object>
                {
                    ["Name"] = dto.Name,
                    ["PipeName"] = dto.LineName,
                    ["ProjectName"] = dto.ProjectName,
                }
            };
        }
        //public static LineDrawRequest ToHotTapRequest(CrossingHotTapDTO dto)
        //{
        //    return new LineDrawRequest
        //    {
        //        TargetLineLayerName = "pipes",

        //        Attributes = new Dictionary<string, object>
        //        {
        //            ["Name"] = dto.Name,
        //            ["PipeName"] = dto.LineName,
        //            ["ProjectName"] = dto.ProjectName,
        //        }
        //    };
        //}
    }
}
