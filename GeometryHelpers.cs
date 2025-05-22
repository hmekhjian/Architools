using Eto.Drawing;
using Rhino;
using Rhino.Geometry;
using System;
using System.IO;

namespace Architools
{
    internal static class GeometryHelpers
    {
        internal static Curve OffsetOpenPolyline(Curve curve, Plane plane, double distance, double tolerance,string alignment)
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

                Curve[] CurveCollection = new Curve[] { OffsetCurveLeft, OffsetCurveRight, CurveSegmentLeft, CurveSegmentRight };
                Curve[] OffsetResultArray = Curve.JoinCurves(CurveCollection);
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
                Curve[] CurveCollection = new Curve[] { curve, OffsetCurve, LineSegment1, LineSegment2 };
                Curve[] OffsetResultArray = Curve.JoinCurves(CurveCollection);
                OffsetResult = OffsetResultArray[0];
            }

            return OffsetResult;
    }

        internal static Curve[] OffsetClosedPolyline(Curve curve, Plane plane, double distance, double tolerance, string alignment, bool Cap = true)
        {
            if (curve == null)
            {
                throw new ArgumentNullException(nameof(curve), "Input curve cannot be null");
            }

            if (!curve.IsClosed)
            {
                throw new ArgumentException("Input curve must be closed", nameof(curve));
            }
            Curve[] OffsetResult = null;
            if (alignment == "Centre")
            {
                double halfDistance = distance / 2.0;
                var ExteriorCurveArray = curve.Offset(plane, halfDistance, tolerance, CurveOffsetCornerStyle.Sharp);
                var ExteriorCurve = ExteriorCurveArray[0];
                var InteriorCurveArray = curve.Offset(plane, -halfDistance, tolerance, CurveOffsetCornerStyle.Sharp);
                var InteriorCurve = InteriorCurveArray[0];
                OffsetResult = new Curve[] { ExteriorCurve, InteriorCurve };
                return OffsetResult;
            }

            else
            {
                if (alignment == "Interior")
                {
                    distance = -distance;
                }

                var OffsetCurveArray = curve.Offset(plane, distance, tolerance, CurveOffsetCornerStyle.Sharp);
                var OffsetCurve = OffsetCurveArray[0];
                OffsetResult = (alignment == "Interior") ? new Curve[] { OffsetCurve, curve } : new Curve[] { curve, OffsetCurve };
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
