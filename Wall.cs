using Rhino;
using Rhino.Commands;
using Rhino.Display;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;
using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;

namespace Architools
{
    public class CreateWall : Command
    {
        public CreateWall()
        {
            Instance = this;
        }

        ///<summary>The only instance of this command.</summary>
        public static CreateWall Instance { get; private set; }

        public override string EnglishName => "CreateWall";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
const string HEIGHT_KEY = "CreateWall_Height";
const string THICKNESS_KEY = "CreateWall_Thickness";
const string ALIGNMENT_KEY = "CreateWall_Alignment";
const string DELETE_INPUT_KEY = "CreateWall_DeleteInput";


           double height = 3000;
           ArchitoolsPlugin.Instance.Settings.TryGetDouble(HEIGHT_KEY, out height);
           double thickness = 300;
           ArchitoolsPlugin.Instance.Settings.TryGetDouble(THICKNESS_KEY, out thickness);
           string alignment = "Centre";
           ArchitoolsPlugin.Instance.Settings.TryGetString(ALIGNMENT_KEY, out alignment);
           bool deleteInput = false;
           ArchitoolsPlugin.Instance.Settings.TryGetBool(DELETE_INPUT_KEY, out deleteInput);

            // Define command option types
            Rhino.Input.Custom.OptionDouble heighOption = new Rhino.Input.Custom.OptionDouble(height, 0, 10000);
            Rhino.Input.Custom.OptionDouble thicknessOption = new Rhino.Input.Custom.OptionDouble(thickness, 0, 1000);
            string[] listValues = new string[] { "Centre", "Interior", "Exterior" };
            int listIndex = Array.IndexOf(listValues, alignment);
            if (listIndex == -1) listIndex = 0;
            Rhino.Input.Custom.OptionToggle deleteInputToggle = new Rhino.Input.Custom.OptionToggle(deleteInput, "False", "True");
            Rhino.Input.Custom.GetPoint getPoints = new Rhino.Input.Custom.GetPoint();
            getPoints.AcceptNothing(true);

            List<Point3d> points = new List<Point3d>();
            string selectedAlignment = listValues[0];
            bool useExistingCurve = false;
            ObjRef inputObject = null;  
            Curve inputCurve = null;
            Brep Wall = null;


            //RhinoApp.WriteLine($"Debug RunCommand: Initial selectedAlignment: '{selectedAlignment}'");
            // TODO Fix dynamic drawing to show the actual 3D volume
            System.EventHandler<Rhino.Input.Custom.GetPointDrawEventArgs> dynamicDrawHandler = (sender, e_draw) =>
            {
                List<Point3d> previewPoints = new List<Point3d>(points);
                if (points.Count > 0)
                {
                    previewPoints.Add(e_draw.CurrentPoint);
                }

                if (previewPoints.Count >= 2) // This check ensures at least one segment can be drawn
                {
                    e_draw.Display.DrawPolyline(previewPoints, System.Drawing.Color.DarkRed, 2);
                }

                // Optionally, draw all individually picked points for better visual feedback.
                foreach (Point3d pt in points)
                {
                    e_draw.Display.DrawPoint(pt, PointStyle.ControlPoint, 3, System.Drawing.Color.Blue);
                }

                // Optionally, draw the current mouse cursor point if a polyline is being actively drawn.
                if (points.Count > 0)
                {
                    e_draw.Display.DrawPoint(e_draw.CurrentPoint, PointStyle.Chevron, 3, System.Drawing.Color.Green);
                }
                // If you want to always show the current mouse point, even before the first pick:
                // else { e_draw.Display.DrawPoint(e_draw.CurrentPoint, PointStyle.Cross, 2, System.Drawing.Color.LightGray); }
            };


            //Subscribe to dynamic draw event handler
            getPoints.DynamicDraw += dynamicDrawHandler;

            //While loop to collect points for the creation of the wall
            while (true)
            {
                string prompt = (points.Count == 0)
                    ? "Select first point of the wall"
                    : "Select next point of the wall or press Enter when done";
                getPoints.SetCommandPrompt(prompt);
                getPoints.DynamicDraw += (sender
                    , e) => e.Display.DrawPolyline(points, System.Drawing.Color.DarkRed);


                //Add options to the command instance
                getPoints.ClearCommandOptions();
                getPoints.AddOptionDouble("Height", ref heighOption);
                getPoints.AddOptionDouble("Thickness", ref thicknessOption);
                int optList = getPoints.AddOptionList("Alignment", listValues, listIndex);
                int optFromCurve = getPoints.AddOption("FromCurve");

                GetResult getResult = getPoints.Get();

                if (getResult == GetResult.Point)
                {
                    points.Add(getPoints.Point());
                    doc.Views.Redraw();
                    continue;
                }
                else if (getResult == GetResult.Option)
                {
                    if (getPoints.Option().Index == optList)
                    {
                        selectedAlignment = listValues[getPoints.Option().CurrentListOptionIndex];
                        listIndex = getPoints.Option().CurrentListOptionIndex;
                        // RhinoApp.WriteLine($"Debug RunCommand: selectedAlignment updated to '{selectedAlignment}' after option selection.");
                        continue;
                    }

                    else if (getPoints.Option().Index == optFromCurve)
                    {
                        useExistingCurve = true;
                        break;
                    }
                }
                else if (getResult == GetResult.Nothing)
                {
                    // RhinoApp.WriteLine($"Debug RunCommand: GetResult.Nothing received. Breaking loop. Final selectedAlignment before break: '{selectedAlignment}'");
                    break;
                }
                else if (getResult == GetResult.Cancel)
                {
// Before exiting, save the current settings
                    ArchitoolsPlugin.Instance.Settings.SetDouble(HEIGHT_KEY, heighOption.CurrentValue);
                    ArchitoolsPlugin.Instance.Settings.SetDouble(THICKNESS_KEY, thicknessOption.CurrentValue);
                    ArchitoolsPlugin.Instance.Settings.SetString(ALIGNMENT_KEY, selectedAlignment);
                    ArchitoolsPlugin.Instance.Settings.SetBool(DELETE_INPUT_KEY, deleteInputToggle.CurrentValue);

                    
                    RhinoApp.WriteLine("Command was cancelled");
                    return Result.Cancel;
                }
                else
                {
                    RhinoApp.WriteLine($"Unexpected input result: {getResult}");
                    return Result.Failure;
                }
            }

            // Unsubscribe from event handler
            getPoints.DynamicDraw -= dynamicDrawHandler;

            //RhinoApp.WriteLine($"Debug RunCommand: Loop broken. selectedAlignment before calling OffsetPolyline: '{selectedAlignment}'");

            if (useExistingCurve)
            {
                Rhino.Input.Custom.GetObject getObject = new Rhino.Input.Custom.GetObject();
                getObject.SetCommandPrompt("Select existing curve for wall path");
                getObject.GeometryFilter = Rhino.DocObjects.ObjectType.Curve;
                getObject.DeselectAllBeforePostSelect = false;
                getObject.EnablePreSelect(true, true);
                getObject.AddOptionToggle("DeleteInput", ref deleteInputToggle);
               
                while (true)
                {
                    int optList = getObject.AddOptionList("Alignment", listValues, listIndex);


                    GetResult getObjResult = getObject.Get();


                    if (getObjResult == GetResult.Object)
                    {
                        inputObject = getObject.Object(0);
                        inputCurve = getObject.Object(0).Curve();
                        if (inputCurve != null)
                            break;
                    }

                    else if (getObjResult == GetResult.Option)
                    {
                        if (getObject.Option().Index == optList)
                        {
                            selectedAlignment = listValues[getObject.Option().CurrentListOptionIndex];
                            listIndex = getObject.Option().CurrentListOptionIndex;
                        }
                        continue;
                    }

                    else if (getObjResult == GetResult.Cancel)
                    {
                        // Before exiting, save the current settings
                    ArchitoolsPlugin.Instance.Settings.SetDouble(HEIGHT_KEY, heighOption.CurrentValue);
                    ArchitoolsPlugin.Instance.Settings.SetDouble(THICKNESS_KEY, thicknessOption.CurrentValue);
                    ArchitoolsPlugin.Instance.Settings.SetString(ALIGNMENT_KEY, selectedAlignment);
                    ArchitoolsPlugin.Instance.Settings.SetBool(DELETE_INPUT_KEY, deleteInputToggle.CurrentValue);

                        RhinoApp.WriteLine("Command was cancelled");
                        return Result.Cancel;
                    }
                    else
                    {
                        RhinoApp.WriteLine("Unexpected input result");
                        return Result.Failure;
                    }

                }

            }

            else
            {

                //Check if enough points were selected
                if (points.Count < 2)
                {
                    RhinoApp.WriteLine("Not enough points selected to generate wall, at least 2 points are required");
                    return Result.Failure;

                }

                inputCurve = new PolylineCurve(points);
            }

            if (inputCurve.IsClosed)
            {
                Curve[] WallPathOffsets = GeometryHelpers.OffsetClosedPolyline(inputCurve, Plane.WorldXY, thicknessOption.CurrentValue, 0.1, selectedAlignment);
                Wall = GeometryHelpers.ExtrudeClosedCurves(WallPathOffsets, Plane.WorldXY, heighOption.CurrentValue, true, selectedAlignment);

            }
            else
            {
                Curve WallPathOffset = GeometryHelpers.OffsetOpenPolyline(inputCurve, Plane.WorldXY, thicknessOption.CurrentValue, 0.1, selectedAlignment);
                Wall = GeometryHelpers.ExtrudeOpenCurve(WallPathOffset, Plane.WorldXY, heighOption.CurrentValue, true).ToBrep();
            }

            // Add created objects to the document
            if (Wall != null) // Check if extrusion succeeded
            {
                Brep wallBrep = Wall; // Convert Extrusion to Brep
                if (wallBrep != null)
                {
                    doc.Objects.AddBrep(wallBrep); // Use AddBrep or Add(Brep)
                    if (deleteInputToggle.CurrentValue)
                        { RhinoDoc.ActiveDoc.Objects.Delete(inputObject.ObjectId, false); }
                    doc.Views.Redraw();
                }
                else
                {
                    RhinoApp.WriteLine("Failed to convert extrusion to Brep.");
                }
            }
            else
            {
                RhinoApp.WriteLine("Failed to create wall extrusion.");
            }


            // Before exiting, save the current settings
                    ArchitoolsPlugin.Instance.Settings.SetDouble(HEIGHT_KEY, heighOption.CurrentValue);
                    ArchitoolsPlugin.Instance.Settings.SetDouble(THICKNESS_KEY, thicknessOption.CurrentValue);
                    ArchitoolsPlugin.Instance.Settings.SetString(ALIGNMENT_KEY, selectedAlignment);
                    ArchitoolsPlugin.Instance.Settings.SetBool(DELETE_INPUT_KEY, deleteInputToggle.CurrentValue);

            return Result.Success;

        }
    }
}


