using System;
using Rhino.Geometry;

namespace Architools
{
    internal static class GeometryHelpers
    {
        internal static Curve OffsetPolyline(Curve curve, Plane plane, double distance, double tolerance)
        {
            Curve[] OffsetCurveArray = curve.Offset(plane, distance, tolerance, CurveOffsetCornerStyle.Sharp);
            Curve OffsetCurve = OffsetCurveArray[0];

            return OffsetCurve;
        }

        internal static Extrusion ExtrudeCurve(Curve curve, Plane plane, double height, bool cap)
        {
            Extrusion ExtrusionResult = Extrusion.Create(curve, plane, height, cap);
            return ExtrusionResult;
        }
    }
}
