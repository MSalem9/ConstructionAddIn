using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConstructionAddIn.Helpers.LineHelpers
{
    public static class LineDrawContext
    {
        public static LineDrawRequest CurrentRequest { get; set; }

        public static void Clear()
        {
            CurrentRequest = null;
        }
    }
}
