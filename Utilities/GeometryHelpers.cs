using Eto.Drawing;
using Rhino;
using Rhino.Geometry;
using System;
using System.IO;


// TODO Set all tolerances to the document tolerance not the static 0.1 value
namespace Architools.Utilities
{
    internal static class GeometryHelpers
    {
        internal static Curve OffsetOpenPolyline(Curve curve, Plane plane, double distance, double tolerance, string alignment)
        {
            RhinoApp.WriteLine($"Alignment received in OffsetPolyline: '{alignment}'");
            Curve OffsetResult = null;

            
            if (curve == null || curve.GetLength() < tolerance)
            {
                return null;
            }

            if (alignment == "Centre")
            {
                double halfDistance = distance / 2.0;
                Curve[] OffsetCurveArrayLeft = curve.Offset(plane, halfDistance, tolerance, CurveOffsetCornerStyle.Sharp);

                // If the offset fails, OffsetCurveArrayLeft will be null or empty.
                if (OffsetCurveArrayLeft == null || OffsetCurveArrayLeft.Length < 1)
                {
                    return null; 
                }
                Curve OffsetCurveLeft = OffsetCurveArrayLeft[0];

                Curve[] OffsetCurveArrayRight = curve.Offset(plane, -halfDistance, tolerance, CurveOffsetCornerStyle.Sharp);

                if (OffsetCurveArrayRight == null || OffsetCurveArrayRight.Length < 1)
                {
                    return null;                }
                Curve OffsetCurveRight = OffsetCurveArrayRight[0];

                Curve CurveSegmentLeft = new Line(OffsetCurveLeft.PointAtStart, OffsetCurveRight.PointAtStart).ToNurbsCurve();
                Curve CurveSegmentRight = new Line(OffsetCurveLeft.PointAtEnd, OffsetCurveRight.PointAtEnd).ToNurbsCurve();

                Curve[] CurveCollection = new Curve[] { OffsetCurveLeft, OffsetCurveRight, CurveSegmentLeft, CurveSegmentRight };
                Curve[] OffsetResultArray = Curve.JoinCurves(CurveCollection);

                if (OffsetResultArray == null || OffsetResultArray.Length < 1)
                {
                    return null;
                }
                OffsetResult = OffsetResultArray[0];
            }
            else 
            {
                if (alignment == "Interior")
                {
                    distance = -distance;
                }
                Curve[] OffsetCurveArray = curve.Offset(plane, -distance, tolerance, CurveOffsetCornerStyle.Sharp);

                if (OffsetCurveArray == null || OffsetCurveArray.Length < 1)
                {
                    return null; 
                }
                Curve OffsetCurve = OffsetCurveArray[0];

                Curve LineSegment1 = new Line(curve.PointAtStart, OffsetCurve.PointAtStart).ToNurbsCurve();
                Curve LineSegment2 = new Line(curve.PointAtEnd, OffsetCurve.PointAtEnd).ToNurbsCurve();
                Curve[] CurveCollection = new Curve[] { curve, OffsetCurve, LineSegment1, LineSegment2 };
                Curve[] OffsetResultArray = Curve.JoinCurves(CurveCollection);

                if (OffsetResultArray == null || OffsetResultArray.Length < 1)
                {
                    return null;
                }
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
                // TODO Implement CW and CWW detection on this alignment to keep consisten t ouptout simlar to below.
            {
                double halfDistance = distance / 2.0;
                var ExteriorCurveArray = curve.Offset(plane, halfDistance, tolerance, CurveOffsetCornerStyle.Sharp);
                var ExteriorCurve = ExteriorCurveArray[0];
                var InteriorCurveArray = curve.Offset(plane, -halfDistance, tolerance, CurveOffsetCornerStyle.Sharp);
                var InteriorCurve = InteriorCurveArray[0];

                //Debug curve direction
                //RhinoDoc.ActiveDoc.Objects.AddCurve(ExteriorCurve);
                //RhinoDoc.ActiveDoc.Objects.AddCurve(InteriorCurve);
                //RhinoApp.WriteLine($"External curve direction is: {ExteriorCurve.ClosedCurveOrientation()}");
                //RhinoApp.WriteLine($"Internal curve direction is: {InteriorCurve.ClosedCurveOrientation()}");
                //RhinoDoc.ActiveDoc.Views.Redraw();

                OffsetResult = new Curve[] { ExteriorCurve, InteriorCurve };
                return OffsetResult;
            }

            else

            {
                if (curve.ClosedCurveOrientation().ToString() == "Clockwise")
                {

                    if (alignment == "Exterior")
                    {
                        distance = -distance;
                    }

                    var OffsetCurveArray = curve.Offset(plane, distance, tolerance, CurveOffsetCornerStyle.Sharp);
                    var OffsetCurve = OffsetCurveArray[0];
                    OffsetResult = new Curve[] { OffsetCurve, curve };

                    //RhinoApp.WriteLine($"OffsetCurve curve direction is: {OffsetCurve.ClosedCurveOrientation()}");
                    //RhinoApp.WriteLine($"Curve curve direction is: {curve.ClosedCurveOrientation()}");
                }

                else if (curve.ClosedCurveOrientation().ToString() == "CounterClockwise")
                {

                    if (alignment == "Interior")
                    {
                        distance = -distance;
                    }

                    var OffsetCurveArray = curve.Offset(plane, distance, tolerance, CurveOffsetCornerStyle.Sharp);
                    var OffsetCurve = OffsetCurveArray[0];
                    OffsetResult = new Curve[] { OffsetCurve, curve };
                }
                return OffsetResult;
            }
        }



        internal static Extrusion ExtrudeOpenCurve(Curve curve, Plane plane, double height, bool cap)
        {
            Extrusion ExtrusionResult = Extrusion.Create(curve, plane, height, cap);
            return ExtrusionResult;
        }

        internal static Brep ExtrudeClosedCurves(Curve[] inputCurves, Plane plane, double height, bool cap, string alignment)
        {
            Brep OffsetExtrusion = Extrusion.Create(inputCurves[0], plane, height, cap).ToBrep();
            Brep CurveExtrusion = Extrusion.Create(inputCurves[1], plane, height, cap).ToBrep();
            Brep[] ExtrusionResultArray = null;

            ExtrusionResultArray = Brep.CreateBooleanDifference(OffsetExtrusion, CurveExtrusion, 0.1);

            if (ExtrusionResultArray == null || ExtrusionResultArray.Length < 1)
            {
                RhinoApp.WriteLine("Warning: Wall creation failed. The boolean difference resulted in no objects.");
                return null;
            }

            Brep ExtrusionResult = ExtrusionResultArray[0];
            return ExtrusionResult;
        }
    }
}
