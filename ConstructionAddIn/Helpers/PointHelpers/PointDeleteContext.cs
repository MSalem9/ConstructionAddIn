using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

    namespace ConstructionAddIn.Helpers.PointHelpers
    {
        /// <summary>
        /// Stores the active delete request.
        ///
        /// The UI/ViewModel sets CurrentRequest before starting the generic
        /// RemovePointFeatureTool workflow.
        /// </summary>
        public static class PointDeleteContext
        {
            /// <summary>
            /// The currently active point delete request.
            /// Null means no delete workflow is active.
            /// </summary>
            public static PointDeleteRequest CurrentRequest { get; set; }
        }
    }

