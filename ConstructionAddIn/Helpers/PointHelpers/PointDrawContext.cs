using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConstructionAddIn.Helpers.PointHelpers
{
    /// <summary>
    /// Stores the active point drawing request.
    ///
    /// The UI/ViewModel sets CurrentRequest before activating the generic
    /// CreatePointFeatureTool.
    ///
    /// The map tool then reads this request and creates the correct feature type.
    /// </summary>
    public static class PointDrawContext
    {
        /// <summary>
        /// The currently active point creation request.
        /// Null means no fitting creation request is active.
        /// </summary>
        public static PointDrawRequest CurrentRequest { get; set; }
    }
}