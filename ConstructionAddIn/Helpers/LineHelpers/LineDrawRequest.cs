using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConstructionAddIn.Helpers.LineHelpers
{
    public class LineDrawRequest
    {
        // The line layer to create into
        public string TargetLineLayerName { get; set; }

        // Attributes coming from form / DTO
        public Dictionary<string, object> Attributes { get; set; } = new();

        // Polygon layer from which extra values will be read
        public string SourcePolygonLayerName { get; set; }

        // Key   = polygon field name
        // Value = target line field name
        public Dictionary<string, string> PolygonFieldMappings { get; set; } = new();

        // If true, values from polygon overwrite same keys in Attributes
        public bool PolygonValuesOverrideExisting { get; set; } = true;

        // If true, the tool will allow the user to set the line length freely.
        // If false, it will be fixed to the value of FixedLength.
        public bool IsFreeLengthEnabled { get; set; }
        public bool IsFreeDimensionEnabled { get; set; }
        public bool IsContinueActive { get; set; }
        public double FixedLength { get; set; }
    }
}
