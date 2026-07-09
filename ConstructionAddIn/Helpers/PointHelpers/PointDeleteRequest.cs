using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConstructionAddIn.Helpers.PointHelpers
{
    public class PointDeleteRequest
    {
        public string TargetLayerName { get; set; }

        public string ConfirmationTitle { get; set; } = "Delete Feature";

        public string ConfirmationMessage { get; set; } = "Are you sure you want to delete the selected feature?";

        public string LayerNotFoundMessage { get; set; } = "Target layer was not found.";

        public string NothingSelectedMessage { get; set; } = "Please select one feature to delete.";

        public string MoreThanOneSelectedMessage { get; set; } = "Please select only one feature.";
    }
}
