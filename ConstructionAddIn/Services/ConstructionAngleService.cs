//using ArcGIS.Core.Geometry;
//using System;

//namespace ConstructionAddIn.Services
//{
//    public static class ConstructionAngleService
//    {
//        public static double GetGeographicAngle(Geometry pipeGeometry, MapPoint clickedPoint)
//        {
//            if (pipeGeometry is not Polyline polyline || clickedPoint == null)
//                return 0;

//            double bestDistance = double.MaxValue;
//            double bestAngle = 0;

//            foreach (var part in polyline.Parts)
//            {
//                foreach (var segment in part)
//                {
//                    var p1 = segment.StartCoordinate;
//                    var p2 = segment.EndCoordinate;

//                    var start = MapPointBuilderEx.CreateMapPoint(p1.X, p1.Y, polyline.SpatialReference);
//                    var end = MapPointBuilderEx.CreateMapPoint(p2.X, p2.Y, polyline.SpatialReference);

//                    double distance = DistancePointToSegment(clickedPoint, start, end);

//                    if (distance < bestDistance)
//                    {
//                        bestDistance = distance;
//                        bestAngle = GetSegmentGeographicAngle(start, end);
//                    }
//                }
//            }

//            return NormalizeAngle(bestAngle);
//        }

//        private static double GetSegmentGeographicAngle(MapPoint p1, MapPoint p2)
//        {
//            double dx = p2.X - p1.X;
//            double dy = p2.Y - p1.Y;

//            // Compute angle in radians relative to North (y-axis)
//            double radians = Math.Atan2(dx, dy); // dx first because North=0° convention
//            double degrees = radians * 180.0 / Math.PI;

//            return NormalizeAngle(degrees);
//        }

//        public static double NormalizeAngle(double angle)
//        {
//            angle %= 360;

//            if (angle < 0)
//                angle += 360;

//            return angle;
//        }

//        private static double DistancePointToSegment(MapPoint p, MapPoint a, MapPoint b)
//        {
//            double dx = b.X - a.X;
//            double dy = b.Y - a.Y;

//            if (dx == 0 && dy == 0)
//                return Math.Sqrt(Math.Pow(p.X - a.X, 2) + Math.Pow(p.Y - a.Y, 2));

//            double t = ((p.X - a.X) * dx + (p.Y - a.Y) * dy) / ((dx * dx) + (dy * dy));
//            t = Math.Max(0, Math.Min(1, t));

//            double nearestX = a.X + t * dx;
//            double nearestY = a.Y + t * dy;

//            return Math.Sqrt(Math.Pow(p.X - nearestX, 2) + Math.Pow(p.Y - nearestY, 2));
//        }
//    }


//}

using ArcGIS.Core.Geometry;
using System;

namespace ConstructionAddIn.Services
{
    public static class ConstructionAngleService
    {
        public static double GetArithmeticAngle(Geometry pipeGeometry, MapPoint clickedPoint)
        {
            if (pipeGeometry is not Polyline polyline || clickedPoint == null)
                return 0;

            double bestDistance = double.MaxValue;
            double bestAngle = 0;

            foreach (var part in polyline.Parts)
            {
                foreach (var segment in part)
                {
                    var p1 = segment.StartCoordinate;
                    var p2 = segment.EndCoordinate;

                    var start = MapPointBuilderEx.CreateMapPoint(p1.X, p1.Y, polyline.SpatialReference);
                    var end = MapPointBuilderEx.CreateMapPoint(p2.X, p2.Y, polyline.SpatialReference);

                    double distance = DistancePointToSegment(clickedPoint, start, end);

                    if (distance < bestDistance)
                    {
                        bestDistance = distance;
                        bestAngle = GetSegmentArithmeticAngle(start, end);
                    }
                }
            }

            return NormalizeAngle(bestAngle);
        }

        // Arithmetic angle: 0° = East, increases clockwise
        private static double GetSegmentArithmeticAngle(MapPoint p1, MapPoint p2)
        {
            double dx = p2.X - p1.X;
            double dy = p2.Y - p1.Y;

            // Standard Cartesian angle: 0° = East, clockwise
            double radians = Math.Atan2(dy, dx); // y first, x second
            double degrees = radians * 180.0 / Math.PI;

            // Convert to 0-360°
            return NormalizeAngle(degrees);
        }

        private static double NormalizeAngle(double angle)
        {
            angle %= 360;
            if (angle < 0) angle += 360;
            return angle;
        }

        private static double DistancePointToSegment(MapPoint p, MapPoint a, MapPoint b)
        {
            double dx = b.X - a.X;
            double dy = b.Y - a.Y;

            if (dx == 0 && dy == 0)
                return Math.Sqrt(Math.Pow(p.X - a.X, 2) + Math.Pow(p.Y - a.Y, 2));

            double t = ((p.X - a.X) * dx + (p.Y - a.Y) * dy) / (dx * dx + dy * dy);
            t = Math.Max(0, Math.Min(1, t));

            double nearestX = a.X + t * dx;
            double nearestY = a.Y + t * dy;

            return Math.Sqrt(Math.Pow(p.X - nearestX, 2) + Math.Pow(p.Y - nearestY, 2));
        }
    }
}