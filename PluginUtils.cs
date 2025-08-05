using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rhino;
using Rhino.PlugIns;

namespace Architools
{
    internal static class PluginUtils
    {
        public static double ConvertToDocumentUnits(RhinoDoc doc, double valueInMillimeters)
        {
            if (doc == null) return valueInMillimeters;
            UnitSystem docUnits = doc.ModelUnitSystem;
            double scale = RhinoMath.UnitScale(UnitSystem.Millimeters, docUnits);

            return scale * valueInMillimeters;
        }

    }
}
