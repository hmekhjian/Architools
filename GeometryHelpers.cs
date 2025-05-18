using Eto.Drawing;
using Rhino;
using Rhino.Geometry;
using System;
using System.IO;

namespace Architools
{
    internal static class GeometryHelpers
    {
        internal static Curve OffsetPolyline(Curve curve, Plane plane, double distance, double tolerance,string alignment, bool Cap = true)
        {
            RhinoApp.WriteLine($"Alignment received in OffsetPolyline: '{alignment}'");
            Curve OffsetResult = null;
            if (alignment == "Centre")
            {
                double halfDistance= distance / 2.0;
                Curve[] OffsetCurveArrayLeft = curve.Offset(plane, halfDistance, tolerance, CurveOffsetCornerStyle.Sharp);
                Curve OffsetCurveLeft = OffsetCurveArrayLeft[0];

                Curve[] OffsetCurveArrayRight = curve.Offset(plane, -halfDistance, tolerance, CurveOffsetCornerStyle.Sharp);
                Curve OffsetCurveRight = OffsetCurveArrayRight[0];

                Curve CurveSegmentLeft = new Line(OffsetCurveLeft.PointAtStart, OffsetCurveRight.PointAtStart).ToNurbsCurve();
                Curve CurveSegmentRight = new Line(OffsetCurveLeft.PointAtEnd, OffsetCurveRight.PointAtEnd).ToNurbsCurve();

                Curve[] curveCollection = new Curve[] { OffsetCurveLeft, OffsetCurveRight, CurveSegmentLeft, CurveSegmentRight };
                Curve[] OffsetResultArray = Curve.JoinCurves(curveCollection);
                OffsetResult = OffsetResultArray[0];
            }

            else
            {
                if (alignment == "Interior")
                {
                    distance = -distance;
                }
                Curve[] OffsetCurveArray = curve.Offset(plane, -distance, tolerance, CurveOffsetCornerStyle.Sharp);
                Curve OffsetCurve = OffsetCurveArray[0];
                Curve LineSegment1 = new Line(curve.PointAtStart, OffsetCurve.PointAtStart).ToNurbsCurve();
                Curve LineSegment2 = new Line(curve.PointAtEnd, OffsetCurve.PointAtEnd).ToNurbsCurve();
                Curve[] curveCollection = new Curve[] { curve, OffsetCurve, LineSegment1, LineSegment2 };
                Curve[] OffsetResultArray = Curve.JoinCurves(curveCollection);
                OffsetResult = OffsetResultArray[0];
            }

            return OffsetResult;
    }




        internal static Extrusion ExtrudeCurve(Curve curve, Plane plane, double height, bool cap)
        {
            Extrusion ExtrusionResult = Extrusion.Create(curve, plane, height, cap);
            return ExtrusionResult;
        }
    }
}
