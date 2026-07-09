using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
using ArcGIS.Desktop.Layouts;
using ArcGIS.Desktop.Mapping;
using ArcGIS.Desktop.KnowledgeGraph;

namespace ConstructionAddIn.Buttons
{
    internal class ResetExtent : Button
    {
        protected override async void OnClick()
        {
            try
            {
                // Clear the clipping geometry on the MCT
                await QueuedTask.Run(() =>
                {
                    var map = MapView.Active.Map;

                    // 1. Clear the clipping geometry
                    map.ClearClipGeometry();

                    // 2. Remove the graphics layer graphics
                    var graphicsLayer = map.GetLayersAsFlattenedList()
                                            .OfType<GraphicsLayer>()
                                            .FirstOrDefault(l => l.Name == "ClipSquareLayer");
                    if (graphicsLayer != null)
                    {
                        graphicsLayer.ClearSelection();
                        graphicsLayer.RemoveElements();
                    }
                });

                // Zoom to full extent on UI thread
                //var mapView = MapView.Active;
                //if (mapView != null)
                //{
                //    var fullExtent = await QueuedTask.Run(() =>
                //    {
                //        return mapView.Map.GetDefaultExtent();
                //    });

                //    if (fullExtent != null)
                //    {
                //        await mapView.ZoomToAsync(fullExtent, TimeSpan.FromSeconds(1));
                //    }
                //}
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in ResetExtent: {ex.Message}");
            }
        }
    }

}
