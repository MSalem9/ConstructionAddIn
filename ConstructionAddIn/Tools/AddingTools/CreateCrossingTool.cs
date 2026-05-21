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
using ConstructionAddIn.Helpers;
using ConstructionAddIn.Helpers.LineHelpers;
using ConstructionAddIn.Helpers.PolygonHelpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConstructionAddIn.Tools.AddingTools
{
    internal class CreateCrossingTool : MapTool
    {
        #region Constructor

        /// <summary>
        /// Initializes the tool configuration.
        /// </summary>
        public CreateCrossingTool()
        {
            // This tool uses a user sketch to create geometry.
            IsSketchTool = true;

            // The user will draw a line.
            SketchType = SketchGeometryType.AngledEllipse;

            // The sketch geometry is returned in map coordinates.
            SketchOutputMode = SketchOutputMode.Map;

            // Enable snapping while sketching.
            UseSnapping = true;
        }

        #endregion

        #region Tool Lifecycle

        /// <summary>
        /// Called when the tool becomes active.
        ///
        /// This method:
        /// - Enables snapping globally.
        /// - Enables common snap modes.
        /// - Makes the relevant target/source layers snappable when found.
        /// </summary>
        /// <param name="active">True if the tool is being activated.</param>
        protected override async Task OnToolActivateAsync(bool active)
        {
            await QueuedTask.Run(() =>
            {
                EnableSnapping();

                var request = LineDrawContext.CurrentRequest;
                var map = MapView.Active?.Map;

                // No request or no active map means there is nothing useful to prepare.
                if (request == null || map == null)
                    return;

                var allFeatureLayers = GetFeatureLayers(map);

                // Make target line layer snappable.
                SetLayerSnappableIfFound(allFeatureLayers, request.TargetLineLayerName);

                // Make source polygon layer snappable, if one is specified.
                if (!string.IsNullOrWhiteSpace(request.SourcePolygonLayerName))
                    SetLayerSnappableIfFound(allFeatureLayers, request.SourcePolygonLayerName);
            });
        }

        /// <summary>
        /// Called when the user finishes sketching.
        ///
        /// This method validates the sketch, creates the line feature,
        /// saves edits, clears the drawing context, and switches back
        /// to the Explore tool.
        /// </summary>
        /// <param name="geometry">The completed sketch geometry.</param>
        /// <returns>True if the line was successfully created and saved; otherwise false.</returns>

        protected override async Task<bool> OnSketchCompleteAsync(Geometry geometry)
        {
            // Validate that the sketch is a non-empty polygon
            // because AngledEllipse returns Polygon geometry.
            if (geometry is not Polygon polygon || polygon.IsEmpty)
            {
                MessageBox.Show("The sketch is not a valid ellipse.");
                return false;
            }

            // Convert polygon boundary to polyline
            var polyline = PolylineBuilderEx.CreatePolyline(polygon.Parts);

            // Ensure the drawing request exists.
            var request = LineDrawContext.CurrentRequest;
            if (request == null)
            {
                MessageBox.Show("No drawing request was prepared.");
                return false;
            }

            // Create the feature on the MCT
            bool created = await QueuedTask.Run(() =>
                CreateLineFeature(polyline, request));

            if (!created)
                return false;

            // Save edits after successful create
            bool saved = await Project.Current.SaveEditsAsync();

            if (!saved)
            {
                MessageBox.Show(
                    "Line was created, but saving edits failed.",
                    "Save failed");

                return false;
            }

            // Cleanup after success
            LineDrawContext.Clear();

            ClearingFormContext.ResetFormAction?.Invoke();

            // Return to Explore tool
            _ = FrameworkApplication.SetCurrentToolAsync(
                "esri_mapping_exploreTool");

            return true;
        }

        #endregion

        #region Feature Creation

        /// <summary>
        /// Creates a new line feature in the configured target line layer.
        /// </summary>
        /// <param name="polyline">The user-sketched polyline geometry.</param>
        /// <param name="request">The current line drawing request.</param>
        /// <returns>True if the feature was created successfully; otherwise false.</returns>
        private bool CreateLineFeature(Polyline polyline, dynamic request)
        {
            var map = MapView.Active?.Map;
            if (map == null)
            {
                MessageBox.Show("No active map view found.");
                return false;
            }

            var allFeatureLayers = GetFeatureLayers(map);

            // Locate and validate the target line layer.
            var lineLayer = allFeatureLayers.FirstOrDefault(l => l.Name == request.TargetLineLayerName);
            if (lineLayer == null)
            {
                MessageBox.Show($"Line layer '{request.TargetLineLayerName}' was not found.");
                return false;
            }

            if (lineLayer.ShapeType != esriGeometryType.esriGeometryPolyline)
            {
                MessageBox.Show($"Layer '{request.TargetLineLayerName}' is not a line layer.");
                return false;
            }

            // Read optional attributes from a polygon layer at the line end point.
            var polygonAttributes = ReadPolygonAttributesIfNeeded(allFeatureLayers, polyline, request);
            if (polygonAttributes == null)
                return false;

            // Merge request attributes with polygon-derived attributes.
            var mergedAttributes = LineAttributeMergeHelper.MergeAttributes(
                request.Attributes,
                polygonAttributes,
                request.PolygonValuesOverrideExisting);

            // Keep only fields that are editable in the target line layer.
            var editableAttributes = LineToolFieldHelper.FilterEditableAttributes(
                lineLayer,
                mergedAttributes);

            // Add a calculated static attribute.
            // PipeLen is set to the geometry length of the drawn line.

            //editableAttributes["PipeLen"] = polyline.Length; // edit this

            // Build the final attribute dictionary used for feature creation.
            var createAttributes = BuildCreateAttributes(polyline, editableAttributes);

            // Execute the edit operation that creates the feature.
            var operation = new EditOperation
            {
                Name = $"Create line in {request.TargetLineLayerName}"
            };

            operation.Create(lineLayer, createAttributes);

            bool ok = operation.Execute();
            if (!ok)
                MessageBox.Show(operation.ErrorMessage, "Create failed");

            return ok;
        }

        /// <summary>
        /// Reads polygon attributes from the configured source polygon layer, if required.
        /// </summary>
        /// <param name="allFeatureLayers">All feature layers in the active map.</param>
        /// <param name="polyline">The drawn polyline.</param>
        /// <param name="request">The current drawing request.</param>
        /// <returns>
        /// A dictionary of polygon-derived attributes if applicable;
        /// an empty dictionary if polygon reading is not required;
        /// null if validation fails.
        /// </returns>
        private Dictionary<string, object> ReadPolygonAttributesIfNeeded(
            List<FeatureLayer> allFeatureLayers,
            Polyline polyline,
            dynamic request)
        {
            // If there is no polygon source layer or no field mapping configured,
            // then polygon attribute reading is not required.
            if (string.IsNullOrWhiteSpace(request.SourcePolygonLayerName) ||
                request.PolygonFieldMappings == null ||
                request.PolygonFieldMappings.Count == 0)
            {
                return new Dictionary<string, object>();
            }

            // Find the polygon source layer.
            var polygonLayer = allFeatureLayers.FirstOrDefault(l => l.Name == request.SourcePolygonLayerName);
            if (polygonLayer == null)
            {
                MessageBox.Show($"Polygon layer '{request.SourcePolygonLayerName}' was not found.");
                return null;
            }

            // Read mapped fields from the polygon located at the line end point.
            var polygonAttributes = PolygonAttributeReader.ReadFromPolygonAtLineEnd(
                polygonLayer,
                polyline,
                request.PolygonFieldMappings);

            // Fail if no polygon was found or no usable attributes were read.
            if (polygonAttributes.Count == 0)
            {
                MessageBox.Show("No polygon was found at the line end point, or no polygon fields could be read.");
                return null;
            }

            return polygonAttributes;
        }

        /// <summary>
        /// Builds the final create dictionary used by the edit operation.
        /// </summary>
        /// <param name="polyline">The line geometry to create.</param>
        /// <param name="attributes">The attribute values to assign.</param>
        /// <returns>A dictionary containing SHAPE and all feature attributes.</returns>
        private Dictionary<string, object> BuildCreateAttributes(
                Polyline polyline,
                Dictionary<string, object> attributes)
        {
            var createAttributes = new Dictionary<string, object>
            {
                // ArcGIS uses the SHAPE field for geometry.
                ["SHAPE"] = polyline
            };

            foreach (var kvp in attributes)
                createAttributes[kvp.Key] = kvp.Value;

            return createAttributes;
        }

        #endregion

        #region Layer / Map Helpers

        /// <summary>
        /// Returns all feature layers from the map, including layers inside group layers.
        /// </summary>
        /// <param name="map">The active map.</param>
        /// <returns>A flattened list of feature layers.</returns>
        private List<FeatureLayer> GetFeatureLayers(Map map)
        {
            return map
                .GetLayersAsFlattenedList()
                .OfType<FeatureLayer>()
                .ToList();
        }

        /// <summary>
        /// Makes a layer snappable if it exists and is not already snappable.
        /// </summary>
        /// <param name="layers">The list of available feature layers.</param>
        /// <param name="layerName">The name of the layer to update.</param>
        private void SetLayerSnappableIfFound(List<FeatureLayer> layers, string layerName)
        {
            var layer = layers.FirstOrDefault(l => l.Name == layerName);
            if (layer != null && !layer.IsSnappable)
                layer.SetSnappable(true);
        }

        /// <summary>
        /// Enables snapping globally and activates the commonly used snap modes.
        /// </summary>
        private void EnableSnapping()
        {
            Snapping.IsEnabled = true;
            Snapping.SetSnapMode(SnapMode.Vertex, true);
            Snapping.SetSnapMode(SnapMode.Edge, true);
            Snapping.SetSnapMode(SnapMode.End, true);
            Snapping.SetSnapMode(SnapMode.Midpoint, true);
            Snapping.SetSnapMode(SnapMode.Intersection, true);
        }

        #endregion
    }
}
