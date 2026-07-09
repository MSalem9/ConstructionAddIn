using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Mapping;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ConstructionAddIn.Helpers.PointHelpers
{
    /// <summary>
    /// Describes everything the generic point creation tool needs in order to create
    /// one construction point feature.
    ///
    /// This class keeps the drawing tool generic. Feature-specific logic should be
    /// passed through this request instead of creating a separate map tool for each feature.
    /// </summary>
    public class PointDrawRequest
    {
        /// <summary>
        /// The name of the target point feature layer where the new feature will be created.
        /// Example:
        /// "ValveRegulator", "ValvePecat", "BallValve1Vent".
        /// </summary>
        public string TargetLayerName { get; set; }

        /// <summary>
        /// Static or pre-known attributes that should be written directly to the new feature.
        ///
        /// Example:
        /// TYPE = "Gate Valve"
        /// GateValve_Desc = " - - - "
        /// Gasket_qty = 2
        /// </summary>
        public Dictionary<string, object> Attributes { get; set; } = new();

        /// <summary>
        /// Optional polygon field mapping.
        ///
        /// Key   = source polygon field name.
        /// Value = target point feature field name.
        ///
        /// Use this later if a construction feature needs values from a polygon layer.
        /// </summary>
        public Dictionary<string, string> PolygonFieldMappings { get; set; } = new();

        /// <summary>
        /// Optional intersecting feature field mapping.
        ///
        /// Key format:
        /// "LayerName.FieldName"
        ///
        /// Value:
        /// target point feature field name.
        ///
        /// Example:
        /// "Sector.SCRN" -> "SCRN"
        /// </summary>
        public Dictionary<string, string> IntersectingFieldMappings { get; set; } = new();

        /// <summary>
        /// Feature-specific callback that runs before the feature is created.
        ///
        /// This is where each construction feature should:
        /// - find the pipe under the clicked point
        /// - read pipe DIAMETER
        /// - read Pipe Name
        /// - read ProjectName
        /// - calculate ANGLE
        /// - apply feature-specific quantity/size logic
        ///
        /// Return null to cancel feature creation.
        /// </summary>
        public Func<MapPoint, Task<Dictionary<string, object>>> BeforeCreateAttributes { get; set; }

        /// <summary>
        /// Optional callback to apply or refresh symbology for the target layer.
        ///
        /// Example:
        /// PointSymbologyService.ApplyValveRegulatorSymbol
        /// </summary>
        public Action<FeatureLayer> ApplySymbologyAction { get; set; }

        /// <summary>
        /// If true, the create tool remains active after creating one feature.
        /// This is useful when the user wants to place multiple fittings.
        /// </summary>
        public bool KeepToolActiveAfterCreate { get; set; } = true;
    }
}