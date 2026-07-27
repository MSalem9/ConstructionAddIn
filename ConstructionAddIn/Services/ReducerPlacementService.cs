using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Framework.Dialogs;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ConstructionAddIn.Services
{
    public static class ReducerPlacementService
    {
        public static async Task<Dictionary<string, object>> BuildAttributesAsync(MapPoint mapPoint)
        {
            return await QueuedTask.Run(() =>
            {
                var attributes = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

                var map = MapView.Active?.Map;
                if (map == null)
                    return null;

                var pipesLayer = map.GetLayersAsFlattenedList()
                    .OfType<FeatureLayer>()
                    .FirstOrDefault(l =>
                        l.Name.Equals("Pipes", StringComparison.OrdinalIgnoreCase));

                if (pipesLayer == null)
                {
                    MessageBox.Show("Pipes layer was not found.");
                    return null;
                }

                var connectedPipes = FindConnectedPipes(pipesLayer, mapPoint);

                if (connectedPipes.Count == 0)
                {
                    MessageBox.Show("Reducer must be placed on or near a pipe.");
                    return null;
                }

                var firstPipe = connectedPipes[0];

                string inputDiameter = null;
                string outputDiameter = null;

                if (connectedPipes.Count >= 2)
                {
                    inputDiameter = connectedPipes[0].Diameter;
                    outputDiameter = connectedPipes[1].Diameter;
                }

                var angle = ConstructionAngleService.GetArithmeticAngle(
                    firstPipe.Geometry,
                    mapPoint,
                    0);

                attributes["CODE"] = null;
                attributes["FUSION"] = null;

                attributes["IPDIAMETER"] = inputDiameter;
                attributes["OPDIAMETER"] = outputDiameter;

                attributes["TYPE"] =
                    inputDiameter != null && outputDiameter != null
                        ? $"{inputDiameter} * {outputDiameter}"
                        : null;

                attributes["ANGLE"] = angle;
                attributes["PipeName"] = firstPipe.PipeName;
                attributes["ProjectName"] = firstPipe.ProjectName;

                return attributes;
            });
        }

        private static List<ReducerPipeInfo> FindConnectedPipes(
            FeatureLayer pipesLayer,
            MapPoint mapPoint)
        {
            var result = SearchPipes(pipesLayer, mapPoint);

            if (result.Count > 0)
                return result;

            var tolerance = GetSearchTolerance(mapPoint);
            var buffer = GeometryEngine.Instance.Buffer(mapPoint, tolerance);

            return SearchPipes(pipesLayer, buffer);
        }

        private static List<ReducerPipeInfo> SearchPipes(
            FeatureLayer pipesLayer,
            Geometry searchGeometry)
        {
            var pipes = new List<ReducerPipeInfo>();

            var filter = new SpatialQueryFilter
            {
                FilterGeometry = searchGeometry,
                SpatialRelationship = SpatialRelationship.Intersects
            };

            using var cursor = pipesLayer.Search(filter);

            while (cursor.MoveNext())
            {
                using var feature = cursor.Current as Feature;

                if (feature == null)
                    continue;

                var geometry = feature.GetShape();

                var diameter =
                    GetValue(feature, "DIAMETER") ??
                    GetValue(feature, "Diameter") ??
                    GetValue(feature, "Diam");

                var pipeName =
                    GetValue(feature, "Name") ??
                    GetValue(feature, "Pipe_Name") ??
                    GetValue(feature, "Pipe Name");

                var projectName =
                    GetValue(feature, "ProjectName") ??
                    GetValue(feature, "Project Name");

                pipes.Add(new ReducerPipeInfo
                {
                    ObjectId = feature.GetObjectID(),
                    Geometry = geometry,
                    Diameter = diameter,
                    PipeName = pipeName,
                    ProjectName = projectName,
                    Distance = GetDistanceToPipe(geometry, searchGeometry)
                });
            }

            return pipes
                .OrderBy(p => p.Distance)
                .Take(2)
                .ToList();
        }

        private static double GetSearchTolerance(MapPoint point)
        {
            if (point?.SpatialReference == null)
                return 0.05;

            return point.SpatialReference.IsGeographic
                ? 0.00001
                : 0.05;
        }

        private static double GetDistanceToPipe(Geometry pipeGeometry, Geometry searchGeometry)
        {
            try
            {
                return GeometryEngine.Instance.Distance(pipeGeometry, searchGeometry);
            }
            catch
            {
                return 0;
            }
        }

        private static string GetValue(Feature feature, string fieldName)
        {
            using var table = feature.GetTable();
            var definition = table.GetDefinition();

            var exists = definition.GetFields()
                .Any(f => f.Name.Equals(fieldName, StringComparison.OrdinalIgnoreCase));

            if (!exists)
                return null;

            var value = feature[fieldName];

            return value == null || value == DBNull.Value
                ? null
                : value.ToString();
        }

        private class ReducerPipeInfo
        {
            public long ObjectId { get; set; }
            public Geometry Geometry { get; set; }
            public string Diameter { get; set; }
            public string PipeName { get; set; }
            public string ProjectName { get; set; }
            public double Distance { get; set; }
        }
    }
}








//using ArcGIS.Core.Data;
//using ArcGIS.Core.Geometry;
//using ArcGIS.Desktop.Framework.Dialogs;
//using ArcGIS.Desktop.Framework.Threading.Tasks;
//using ArcGIS.Desktop.Mapping;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Threading.Tasks;

//namespace ConstructionAddIn.Services
//{
//    public static class ReducerPlacementService
//    {
//        public static async Task<Dictionary<string, object>> BuildAttributesAsync(MapPoint mapPoint)
//        {
//            return await QueuedTask.Run(() =>
//            {
//                var attributes = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

//                var map = MapView.Active?.Map;
//                if (map == null)
//                    return null;

//                var pipesLayer = map.GetLayersAsFlattenedList()
//                    .OfType<FeatureLayer>()
//                    .FirstOrDefault(l =>
//                        l.Name.Equals("Pipes", StringComparison.OrdinalIgnoreCase));

//                if (pipesLayer == null)
//                {
//                    MessageBox.Show("Pipes layer was not found.");
//                    return null;
//                }

//                var connectedPipes = FindConnectedPipes(pipesLayer, mapPoint);

//                if (connectedPipes.Count == 0)
//                {
//                    MessageBox.Show("Reducer must be placed on or near a pipe.");
//                    return null;
//                }

//                var firstPipe = connectedPipes[0];

//                string inputDiameter = null;
//                string outputDiameter = null;

//                if (connectedPipes.Count >= 2)
//                {
//                    inputDiameter = connectedPipes[0].Diameter;
//                    outputDiameter = connectedPipes[1].Diameter;
//                }

//                var angle = ConstructionAngleService.GetGeographicAngle(
//                    firstPipe.Geometry,
//                    mapPoint);

//                attributes["code"] = null;
//                attributes["fusion"] = null;

//                attributes["IPDIAMETER"] = inputDiameter;
//                attributes["OPDIAMETER"] = outputDiameter;

//                attributes["TYPE"] =
//                    inputDiameter != null && outputDiameter != null
//                        ? $"{inputDiameter} * {outputDiameter}"
//                        : null;

//                attributes["ANGLE"] = angle;
//                attributes["Pipe Name"] = firstPipe.PipeName;
//                attributes["Project Name"] = firstPipe.ProjectName;

//                return attributes;
//            });
//        }

//        private static List<ReducerPipeInfo> FindConnectedPipes(
//            FeatureLayer pipesLayer,
//            MapPoint mapPoint)
//        {
//            var result = SearchPipes(pipesLayer, mapPoint);

//            if (result.Count > 0)
//                return result;

//            var tolerance = GetSearchTolerance(mapPoint);
//            var buffer = GeometryEngine.Instance.Buffer(mapPoint, tolerance);

//            return SearchPipes(pipesLayer, buffer);
//        }

//        private static List<ReducerPipeInfo> SearchPipes(
//            FeatureLayer pipesLayer,
//            Geometry searchGeometry)
//        {
//            var pipes = new List<ReducerPipeInfo>();

//            var filter = new SpatialQueryFilter
//            {
//                FilterGeometry = searchGeometry,
//                SpatialRelationship = SpatialRelationship.Intersects
//            };

//            using var cursor = pipesLayer.Search(filter);

//            while (cursor.MoveNext())
//            {
//                using var feature = cursor.Current as Feature;

//                if (feature == null)
//                    continue;

//                var geometry = feature.GetShape();

//                var diameter =
//                    GetValue(feature, "DIAMETER") ??
//                    GetValue(feature, "Diameter") ??
//                    GetValue(feature, "Diam");

//                var pipeName =
//                    GetValue(feature, "Name") ??
//                    GetValue(feature, "Pipe_Name") ??
//                    GetValue(feature, "Pipe Name");

//                var projectName =
//                    GetValue(feature, "ProjectName") ??
//                    GetValue(feature, "Project Name");

//                pipes.Add(new ReducerPipeInfo
//                {
//                    ObjectId = feature.GetObjectID(),
//                    Geometry = geometry,
//                    Diameter = diameter,
//                    PipeName = pipeName,
//                    ProjectName = projectName,
//                    Distance = GetDistanceToPipe(geometry, searchGeometry)
//                });
//            }

//            return pipes
//                .OrderBy(p => p.Distance)
//                .Take(2)
//                .ToList();
//        }

//        private static double GetSearchTolerance(MapPoint point)
//        {
//            if (point?.SpatialReference == null)
//                return 0.05;

//            return point.SpatialReference.IsGeographic
//                ? 0.00001
//                : 0.05;
//        }

//        private static double GetDistanceToPipe(Geometry pipeGeometry, Geometry searchGeometry)
//        {
//            try
//            {
//                return GeometryEngine.Instance.Distance(pipeGeometry, searchGeometry);
//            }
//            catch
//            {
//                return 0;
//            }
//        }

//        private static string GetValue(Feature feature, string fieldName)
//        {
//            using var table = feature.GetTable();
//            var definition = table.GetDefinition();

//            var exists = definition.GetFields()
//                .Any(f => f.Name.Equals(fieldName, StringComparison.OrdinalIgnoreCase));

//            if (!exists)
//                return null;

//            var value = feature[fieldName];

//            return value == null || value == DBNull.Value
//                ? null
//                : value.ToString();
//        }

//        private class ReducerPipeInfo
//        {
//            public long ObjectId { get; set; }
//            public Geometry Geometry { get; set; }
//            public string Diameter { get; set; }
//            public string PipeName { get; set; }
//            public string ProjectName { get; set; }
//            public double Distance { get; set; }
//        }
//    }
//}


////using ArcGIS.Core.Data;
////using ArcGIS.Core.Geometry;
////using ArcGIS.Desktop.Framework.Dialogs;
////using ArcGIS.Desktop.Framework.Threading.Tasks;
////using ArcGIS.Desktop.Mapping;
////using System;
////using System.Collections.Generic;
////using System.Linq;
////using System.Threading.Tasks;

////namespace ConstructionAddIn.Services
////{
////    public static class ReducerPlacementService
////    {
////        public static async Task<Dictionary<string, object>> BuildAttributesAsync(MapPoint mapPoint)
////        {
////            return await QueuedTask.Run(() =>
////            {
////                var attributes = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

////                var map = MapView.Active?.Map;
////                if (map == null)
////                    return null;

////                var pipesLayer = map.GetLayersAsFlattenedList()
////                    .OfType<FeatureLayer>()
////                    .FirstOrDefault(l =>
////                        l.Name.Equals("Pipes", StringComparison.OrdinalIgnoreCase));

////                if (pipesLayer == null)
////                {
////                    MessageBox.Show("Pipes layer was not found.");
////                    return null;
////                }

////                var connectedPipes = FindConnectedPipes(pipesLayer, mapPoint);

////                if (connectedPipes.Count < 2)
////                {
////                    MessageBox.Show("Reducer must be placed between two connected pipes.");
////                    return null;
////                }

////                var firstPipe = connectedPipes[0];
////                var secondPipe = connectedPipes[1];

////                var inputDiameter = firstPipe.Diameter;
////                var outputDiameter = secondPipe.Diameter;

////                var angle = ConstructionAngleService.GetGeographicAngle(firstPipe.Geometry, mapPoint);

////                attributes["code"] = null;
////                attributes["fusion"] = null;

////                attributes["IPDIAMETER"] = inputDiameter;
////                attributes["OPDIAMETER"] = outputDiameter;
////                attributes["TYPE"] = $"{inputDiameter} * {outputDiameter}";
////                attributes["ANGLE"] = angle;

////                attributes["Pipe Name"] = firstPipe.PipeName;
////                attributes["Project Name"] = firstPipe.ProjectName;

////                return attributes;
////            });
////        }

////        private static List<ReducerPipeInfo> FindConnectedPipes(
////            FeatureLayer pipesLayer,
////            MapPoint mapPoint)
////        {
////            var result = new List<ReducerPipeInfo>();

////            // First try direct intersection
////            result = SearchPipes(pipesLayer, mapPoint);

////            if (result.Count >= 2)
////                return result;

////            // If the snapped point does not exactly intersect both pipes,
////            // use a very small buffer around the clicked point.
////            var tolerance = GetSearchTolerance(mapPoint);
////            var buffer = GeometryEngine.Instance.Buffer(mapPoint, tolerance);

////            result = SearchPipes(pipesLayer, buffer);

////            return result;
////        }

////        private static List<ReducerPipeInfo> SearchPipes(
////            FeatureLayer pipesLayer,
////            Geometry searchGeometry)
////        {
////            var pipes = new List<ReducerPipeInfo>();

////            var filter = new SpatialQueryFilter
////            {
////                FilterGeometry = searchGeometry,
////                SpatialRelationship = SpatialRelationship.Intersects
////            };

////            using var cursor = pipesLayer.Search(filter);

////            while (cursor.MoveNext())
////            {
////                using var feature = cursor.Current as Feature;

////                if (feature == null)
////                    continue;

////                var geometry = feature.GetShape();

////                var diameter =
////                    GetValue(feature, "DIAMETER") ??
////                    GetValue(feature, "Diameter") ??
////                    GetValue(feature, "Diam");

////                var pipeName =
////                    GetValue(feature, "Name") ??
////                    GetValue(feature, "Pipe_Name") ??
////                    GetValue(feature, "Pipe Name");

////                var projectName =
////                    GetValue(feature, "ProjectName") ??
////                    GetValue(feature, "Project Name");

////                pipes.Add(new ReducerPipeInfo
////                {
////                    ObjectId = feature.GetObjectID(),
////                    Geometry = geometry,
////                    Diameter = diameter,
////                    PipeName = pipeName,
////                    ProjectName = projectName,
////                    Distance = GetDistanceToPipe(geometry, searchGeometry)
////                });
////            }

////            return pipes
////                .OrderBy(p => p.Distance)
////                .Take(2)
////                .ToList();
////        }

////        private static double GetSearchTolerance(MapPoint point)
////        {
////            if (point?.SpatialReference == null)
////                return 0.05;

////            return point.SpatialReference.IsGeographic
////                ? 0.00001
////                : 0.05;
////        }

////        private static double GetDistanceToPipe(Geometry pipeGeometry, Geometry searchGeometry)
////        {
////            try
////            {
////                return GeometryEngine.Instance.Distance(pipeGeometry, searchGeometry);
////            }
////            catch
////            {
////                return 0;
////            }
////        }

////        private static string GetValue(Feature feature, string fieldName)
////        {
////            using var table = feature.GetTable();
////            var definition = table.GetDefinition();

////            var exists = definition.GetFields()
////                .Any(f => f.Name.Equals(fieldName, StringComparison.OrdinalIgnoreCase));

////            if (!exists)
////                return null;

////            var value = feature[fieldName];

////            return value == null || value == DBNull.Value
////                ? null
////                : value.ToString();
////        }

////        private class ReducerPipeInfo
////        {
////            public long ObjectId { get; set; }
////            public Geometry Geometry { get; set; }
////            public string Diameter { get; set; }
////            public string PipeName { get; set; }
////            public string ProjectName { get; set; }
////            public double Distance { get; set; }
////        }
////    }
////}


////using ArcGIS.Core.Geometry;
////using ArcGIS.Desktop.Framework.Dialogs;
////using System;
////using System.Collections.Generic;

////namespace ConstructionAddIn.Services
////{
////    public static class ReducerPlacementService
////    {
////        public static Dictionary<string, object> BuildAttributesAsync(MapPoint mapPoint)
////        {
////            var attributes = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

////            var pipe = ConstructionPipeAttributeService
////                .FindPipeAtPointAsync(mapPoint)
////                .Result;

////            if (pipe == null)
////            {
////                MessageBox.Show("Reducer must be drawn on a pipe.");
////                return null;
////            }

////            var diameter = pipe.Diameter;
////            var angle = ConstructionAngleService.GetGeographicAngle(pipe.Geometry, mapPoint);

////            attributes["ANGLE"] = angle;
////            attributes["IPDIAMETER"] = diameter;
////            attributes["OPDIAMETER"] = diameter;
////            attributes["TYPE"] = $"{diameter} * {diameter}";
////            attributes["Pipe Name"] = pipe.PipeName;
////            attributes["Project Name"] = pipe.ProjectName;

////            return attributes;
////        }
////    }
////}