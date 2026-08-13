//using ArcGIS.Core.CIM;
//using ArcGIS.Core.Data;
//using ArcGIS.Core.Geometry;
//using ArcGIS.Desktop.Catalog;
//using ArcGIS.Desktop.Core;
//using ArcGIS.Desktop.Editing;
//using ArcGIS.Desktop.Extensions;
//using ArcGIS.Desktop.Framework;
//using ArcGIS.Desktop.Framework.Contracts;
//using ArcGIS.Desktop.Framework.Dialogs;
//using ArcGIS.Desktop.Framework.Threading.Tasks;
//using ArcGIS.Desktop.KnowledgeGraph;
//using ArcGIS.Desktop.Layouts;
//using ArcGIS.Desktop.Mapping;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using System.Windows;

//namespace ConstructionAddIn.Tools.ServiceTools
//{
//    internal class DrawSquareClipTool : MapTool
//    {
//        public DrawSquareClipTool()
//        {
//        }

//        protected override Task OnToolActivateAsync(bool active)
//        {

//            IsSketchTool = true;
//            SketchType = SketchGeometryType.Polygon;
//            SketchOutputMode = SketchOutputMode.Map;

//            // Show a message when tool is activated - using ArcGIS Pro MessageBox
//            ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show("Draw Square Clip Tool Activated. Click and drag on the map to draw a rectangle.",
//                "Tool Activated", MessageBoxButton.OK, MessageBoxImage.Information);
//            System.Diagnostics.Debug.WriteLine("DrawSquareClipTool activated");

//            return base.OnToolActivateAsync(active);
//        }

//        protected override Task OnToolDeactivateAsync(bool hasMapViewChanged)
//        {
//            System.Diagnostics.Debug.WriteLine("DrawSquareClipTool deactivated");
//            return base.OnToolDeactivateAsync(hasMapViewChanged);
//        }

//        protected override async Task<bool> OnSketchCompleteAsync(Geometry geometry)
//        {
//            System.Diagnostics.Debug.WriteLine("OnSketchCompleteAsync called");

//            if (geometry == null)
//            {
//                System.Diagnostics.Debug.WriteLine("ERROR: Geometry is null");
//                ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show("Error: No geometry drawn", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
//                return false;
//            }

//            try
//            {
//                // Convert geometry to Polygon
//                var squarePolygon = geometry as Polygon;
//                if (squarePolygon == null)
//                {
//                    System.Diagnostics.Debug.WriteLine("ERROR: Geometry is not a Polygon");
//                    ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show("Error: Drawn shape is not a polygon", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
//                    return false;
//                }

//                System.Diagnostics.Debug.WriteLine($"Polygon created with {squarePolygon.Points.Count} points");

//                await QueuedTask.Run(() =>
//                {
//                    try
//                    {
//                        var mapView = MapView.Active;
//                        if (mapView == null)
//                        {
//                            System.Diagnostics.Debug.WriteLine("ERROR: MapView.Active is null");
//                            return;
//                        }

//                        var map = mapView.Map;
//                        if (map == null)
//                        {
//                            System.Diagnostics.Debug.WriteLine("ERROR: Map is null");
//                            return;
//                        }

//                        System.Diagnostics.Debug.WriteLine("Map found successfully");

//                        // 1. Get or create the graphics layer
//                        GraphicsLayer graphicsLayer = null;

//                        // Look for existing layer
//                        foreach (var layer in map.GetLayersAsFlattenedList())
//                        {
//                            if (layer is GraphicsLayer gl && gl.Name == "ClipSquareLayer")
//                            {
//                                graphicsLayer = gl;
//                                System.Diagnostics.Debug.WriteLine("Found existing GraphicsLayer");
//                                break;
//                            }
//                        }

//                        if (graphicsLayer == null)
//                        {
//                            System.Diagnostics.Debug.WriteLine("Creating new GraphicsLayer");
//                            var creationParams = new GraphicsLayerCreationParams()
//                            {
//                                Name = "ClipSquareLayer"
//                            };
//                            graphicsLayer = LayerFactory.Instance.CreateLayer<GraphicsLayer>(creationParams, map);
//                            System.Diagnostics.Debug.WriteLine("Created new GraphicsLayer");
//                        }

//                        // Clear existing graphics
//                        graphicsLayer.ClearSelection();
//                        System.Diagnostics.Debug.WriteLine("Cleared existing graphics");

//                        // 2. Create a symbol for the square - FIX 1: No fill color inside
//                        var outlineStroke = SymbolFactory.Instance.ConstructStroke(
//                            ColorFactory.Instance.RedRGB, 3.0, SimpleLineStyle.Solid);

//                        // Create polygon symbol with COMPLETELY TRANSPARENT fill (no red inside)
//                        var polygonSymbol = SymbolFactory.Instance.ConstructPolygonSymbol(
//                            ColorFactory.Instance.CreateRGBColor(255, 0, 0, 0), // Alpha = 0 means completely transparent
//                            SimpleFillStyle.Solid,
//                            outlineStroke);

//                        // 3. Create and add the graphic element
//                        var graphic = new CIMPolygonGraphic()
//                        {
//                            Symbol = polygonSymbol.MakeSymbolReference(),
//                            Polygon = squarePolygon
//                        };

//                        graphicsLayer.AddElement(graphic);
//                        System.Diagnostics.Debug.WriteLine("Added graphic to layer");

//                        // 4. Clip the map to the square
//                        var clipBorderStroke = SymbolFactory.Instance.ConstructStroke(
//                            ColorFactory.Instance.RedRGB, 2.0, SimpleLineStyle.Dash);
//                        var clipBorderSymbol = SymbolFactory.Instance.ConstructLineSymbol(clipBorderStroke);

//                        map.SetClipGeometry(squarePolygon, clipBorderSymbol);
//                        System.Diagnostics.Debug.WriteLine("Applied clip geometry to map");

//                        // Show success message - using ArcGIS Pro MessageBox
//                        ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show("Map clipped to drawn rectangle!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
//                    }
//                    catch (Exception ex)
//                    {
//                        System.Diagnostics.Debug.WriteLine($"ERROR in QueuedTask: {ex.Message}");
//                        System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
//                        throw;
//                    }
//                    finally
//                    {
//                        _ = FrameworkApplication.SetCurrentToolAsync("esri_mapping_exploreTool");
//                    }
//                });
//            }
//            catch (Exception ex)
//            {
//                System.Diagnostics.Debug.WriteLine($"ERROR in OnSketchCompleteAsync: {ex.Message}");
//                ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
//                return false;
//            }

//            return true;
//        }
//    }
//}

using ArcGIS.Core.CIM;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Catalog;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Editing;
using ArcGIS.Desktop.Extensions;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Framework.Dialogs;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.KnowledgeGraph;
using ArcGIS.Desktop.Layouts;
using ArcGIS.Desktop.Mapping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ConstructionAddIn.Tools.ServiceTools
{
    internal class DrawSquareClipTool : MapTool
    {
        public DrawSquareClipTool()
        {
        }

        protected override Task OnToolActivateAsync(bool active)
        {
            IsSketchTool = true;
            SketchType = SketchGeometryType.Polygon;
            SketchOutputMode = SketchOutputMode.Map;

            ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show(
                "Draw Square Clip Tool Activated. Click and drag on the map to draw a rectangle.",
                "Tool Activated",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            System.Diagnostics.Debug.WriteLine(
                "DrawSquareClipTool activated");

            return base.OnToolActivateAsync(active);
        }

        protected override Task OnToolDeactivateAsync(bool hasMapViewChanged)
        {
            System.Diagnostics.Debug.WriteLine(
                "DrawSquareClipTool deactivated");

            return base.OnToolDeactivateAsync(hasMapViewChanged);
        }

        protected override async Task<bool> OnSketchCompleteAsync(
            Geometry geometry)
        {
            System.Diagnostics.Debug.WriteLine(
                "OnSketchCompleteAsync called");

            if (geometry == null)
            {
                System.Diagnostics.Debug.WriteLine(
                    "ERROR: Geometry is null");

                ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show(
                    "Error: No geometry drawn",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                return false;
            }

            try
            {
                var squarePolygon = geometry as Polygon;

                if (squarePolygon == null)
                {
                    System.Diagnostics.Debug.WriteLine(
                        "ERROR: Geometry is not a Polygon");

                    ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show(
                        "Error: Drawn shape is not a polygon",
                        "Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);

                    return false;
                }

                System.Diagnostics.Debug.WriteLine(
                    $"Polygon created with {squarePolygon.Points.Count} points");

                await QueuedTask.Run(() =>
                {
                    try
                    {
                        var mapView = MapView.Active;

                        if (mapView == null)
                        {
                            System.Diagnostics.Debug.WriteLine(
                                "ERROR: MapView.Active is null");
                            return;
                        }

                        var map = mapView.Map;

                        if (map == null)
                        {
                            System.Diagnostics.Debug.WriteLine(
                                "ERROR: Map is null");
                            return;
                        }

                        System.Diagnostics.Debug.WriteLine(
                            "Map found successfully");


                        // =====================================================
                        // 1. GET OR CREATE GRAPHICS LAYER
                        // =====================================================

                        GraphicsLayer graphicsLayer = null;

                        foreach (var layer in map.GetLayersAsFlattenedList())
                        {
                            if (layer is GraphicsLayer gl &&
                                gl.Name == "ClipSquareLayer")
                            {
                                graphicsLayer = gl;

                                System.Diagnostics.Debug.WriteLine(
                                    "Found existing GraphicsLayer");

                                break;
                            }
                        }

                        if (graphicsLayer == null)
                        {
                            System.Diagnostics.Debug.WriteLine(
                                "Creating new GraphicsLayer");

                            var creationParams =
                                new GraphicsLayerCreationParams()
                                {
                                    Name = "ClipSquareLayer"
                                };

                            graphicsLayer =
                                LayerFactory.Instance
                                    .CreateLayer<GraphicsLayer>(
                                        creationParams,
                                        map);

                            System.Diagnostics.Debug.WriteLine(
                                "Created new GraphicsLayer");
                        }


                        // =====================================================
                        // 2. CREATE COMPLETELY TRANSPARENT SYMBOL
                        //    Fill = No Color
                        //    Border = No Color
                        // =====================================================

                        var transparentColor =
                            ColorFactory.Instance.CreateRGBColor(
                                0,
                                0,
                                0,
                                0); // Alpha 0 = fully transparent

                        var transparentOutline =
                            SymbolFactory.Instance.ConstructStroke(
                                transparentColor,
                                1.0,
                                SimpleLineStyle.Solid);

                        var polygonSymbol =
                            SymbolFactory.Instance.ConstructPolygonSymbol(
                                transparentColor,
                                SimpleFillStyle.Solid,
                                transparentOutline);


                        // =====================================================
                        // 3. ADD CLIP POLYGON GRAPHIC
                        // =====================================================

                        var graphic = new CIMPolygonGraphic()
                        {
                            Symbol =
                                polygonSymbol.MakeSymbolReference(),

                            Polygon = squarePolygon
                        };

                        graphicsLayer.AddElement(graphic);

                        System.Diagnostics.Debug.WriteLine(
                            "Added transparent graphic to layer");


                        // =====================================================
                        // 4. FIND ALL BASEMAP LAYERS
                        //
                        // These will be EXCLUDED from clipping.
                        // Therefore:
                        //
                        // Basemap        -> remains visible everywhere
                        // Operational    -> clipped to square
                        // =====================================================

                        var basemapLayers =
                            map.GetLayersAsFlattenedList()
                                .Where(layer =>
                                    layer.MapLayerType ==
                                        MapLayerType.BasemapBackground
                                    ||
                                    layer.MapLayerType ==
                                        MapLayerType.BasemapTopReference)
                                .ToList();

                        System.Diagnostics.Debug.WriteLine(
                            $"Found {basemapLayers.Count} basemap layer(s) " +
                            "to exclude from clipping.");

                        foreach (var basemapLayer in basemapLayers)
                        {
                            System.Diagnostics.Debug.WriteLine(
                                $"Excluding basemap layer: " +
                                $"{basemapLayer.Name}");
                        }


                        // =====================================================
                        // 5. CREATE "NO COLOR" CLIP BORDER
                        // =====================================================

                        var noColorClipStroke =
                            SymbolFactory.Instance.ConstructStroke(
                                transparentColor,
                                1.0,
                                SimpleLineStyle.Solid);

                        var noColorClipBorder =
                            SymbolFactory.Instance.ConstructLineSymbol(
                                noColorClipStroke);


                        // =====================================================
                        // 6. APPLY MAP CLIPPING
                        //
                        // IMPORTANT:
                        // basemapLayers are EXCLUDED from clipping.
                        //
                        // null = no elevation surfaces excluded.
                        //
                        // noColorClipBorder = invisible clip boundary.
                        // =====================================================

                        map.SetClipGeometry(
                            squarePolygon,
                            basemapLayers,
                            null,
                            noColorClipBorder);

                        System.Diagnostics.Debug.WriteLine(
                            "Applied clip geometry.");

                        System.Diagnostics.Debug.WriteLine(
                            "Basemap excluded from clipping.");

                        System.Diagnostics.Debug.WriteLine(
                            "Clip border set to No Color.");


                        // =====================================================
                        // 7. SUCCESS MESSAGE
                        // =====================================================

                        ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show(
                            "Map clipped successfully.\n\n" +
                            "• Basemap is not clipped\n" +
                            "• Clip border has no color",
                            "Success",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine(
                            $"ERROR in QueuedTask: {ex.Message}");

                        System.Diagnostics.Debug.WriteLine(
                            $"Stack trace: {ex.StackTrace}");

                        throw;
                    }
                    finally
                    {
                        _ = FrameworkApplication.SetCurrentToolAsync(
                            "esri_mapping_exploreTool");
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"ERROR in OnSketchCompleteAsync: {ex.Message}");

                ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show(
                    $"Error: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                return false;
            }

            return true;
        }
    }
}
