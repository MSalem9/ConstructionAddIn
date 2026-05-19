using ArcGIS.Core.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConstructionAddIn.Helpers.LineHelpers
{
    internal static class LineToolGeometryHelper
    {
        public static MapPoint GetLineEndPoint(Polyline polyline)
        {
            if (polyline == null || polyline.IsEmpty)
                return null;

            if (polyline.PointCount == 0)
                return null;

            return polyline.Points[polyline.PointCount - 1];
        }
    }
}
