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
using Architools.Models;
using Architools.Utilities;

namespace Architools.Commands
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

        private enum CommandState
        {
            CollectPoints,
            SelectCurve
        }

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            CommandState currentState = CommandState.CollectPoints;

            WallSettings settings = new WallSettings();
            settings.Load(doc);

            // Define command option types
            Rhino.Input.Custom.OptionDouble heighOption = new Rhino.Input.Custom.OptionDouble(settings.Height);
            Rhino.Input.Custom.OptionDouble thicknessOption = new Rhino.Input.Custom.OptionDouble(settings.Thickness);
            string[] listValues = new string[] { "Centre", "Interior", "Exterior" };
            int listIndex = Array.IndexOf(listValues, settings.Alignment);
            if (listIndex == -1) listIndex = 0;
            Rhino.Input.Custom.OptionToggle deleteInputToggle = new Rhino.Input.Custom.OptionToggle(settings.DeleteInput, "False", "True");
            Rhino.Input.Custom.GetPoint getPoints = new Rhino.Input.Custom.GetPoint();
            getPoints.AcceptNothing(true);

            List<Point3d> points = new List<Point3d>();
            string selectedAlignment = listValues[0];
            bool useExistingCurve = false;
            ObjRef inputObject = null;
            Curve inputCurve = null;
            Brep Wall = null;


            // TODO Fix dynamic drawing to show the actual 3D volume
            System.EventHandler<Rhino.Input.Custom.GetPointDrawEventArgs> dynamicDrawHandler = (sender, e) =>
            {
                List<Point3d> previewPoints = new List<Point3d>(points);
                if (points.Count > 0)
                {
                    previewPoints.Add(e.CurrentPoint);
                }

                if (previewPoints.Count < 2)
                {
                    return;
                }


                try
                {
                    Curve previewCurve = new PolylineCurve(previewPoints);
                    Curve wallPathOffset = GeometryHelpers.OffsetOpenPolyline(previewCurve, Plane.WorldXY, thicknessOption.CurrentValue, doc.ModelAbsoluteTolerance, selectedAlignment);
                    if (wallPathOffset == null)
                    {
                        return; // Nothing to draw, so exit the handler for this frame.
                    }
                    Brep wallPreview = GeometryHelpers.ExtrudeOpenCurve(wallPathOffset, Plane.WorldXY, heighOption.CurrentValue, true).ToBrep();

                    if (wallPreview != null)
                    {
                        e.Display.DrawBrepWires(wallPreview, System.Drawing.Color.DarkRed, 1);
                    }
                }

                catch (Exception ex)
                {
                    RhinoApp.WriteLine($"Error in dynamic draw: {ex.Message}");
                }

            };


            while (true)
            {
                if (currentState == CommandState.CollectPoints)
                {
                    string prompt = (points.Count == 0)
                                       ? "Select first point of the wall"
                                       : "Select next point of the wall or press Enter when done";
                    getPoints.SetCommandPrompt(prompt);
                    // getPoints.DynamicDraw += (sender
                    //    , e) => e.Display.DrawPolyline(points, System.Drawing.Color.DarkRed);


                    //Add options to the command instance
                    getPoints.ClearCommandOptions();
                    getPoints.AddOptionDouble("Height", ref heighOption);
                    getPoints.AddOptionDouble("Thickness", ref thicknessOption);
                    int optList = getPoints.AddOptionList("Alignment", listValues, listIndex);
                    int optFromCurve = getPoints.AddOption("FromCurve");

                    getPoints.DynamicDraw += dynamicDrawHandler;

                    GetResult getResult = getPoints.Get();

                    getPoints.DynamicDraw -= dynamicDrawHandler;


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
                            currentState = CommandState.SelectCurve;
                            continue;
                        }
                    }
                    else if (getResult == GetResult.Nothing)
                    {
                        //Check if enough points were selected
                        if (points.Count < 2)
                        {
                            RhinoApp.WriteLine("Not enough points selected to generate wall, at least 2 points are required");
                            return Result.Failure;
                        }

                        inputCurve = new PolylineCurve(points);

                        break;
                    }
                    else if (getResult == GetResult.Cancel)
                    {
                        // Before exiting, save the current settings
                       settings.Save(); 


                        RhinoApp.WriteLine("Command was cancelled");
                        return Result.Cancel;
                    }
                    else
                    {
                        RhinoApp.WriteLine($"Unexpected input result: {getResult}");
                        return Result.Failure;
                    }






                }

                if (currentState == CommandState.SelectCurve)
                {
                    Rhino.Input.Custom.GetObject getObject = new Rhino.Input.Custom.GetObject();
                    getObject.SetCommandPrompt("Select existing curve for wall path");
                    getObject.GeometryFilter = Rhino.DocObjects.ObjectType.Curve;
                    getObject.DeselectAllBeforePostSelect = false;
                    getObject.EnablePreSelect(true, true);
                    getObject.ClearCommandOptions();
                    getObject.AddOptionDouble("Height", ref heighOption);
                    getObject.AddOptionDouble("Thickness", ref thicknessOption);
                    int optList = getObject.AddOptionList("Alignment", listValues, listIndex);
                    int optDrawPolyline = getObject.AddOption("DrawPolyline");
                    getObject.AddOptionToggle("DeleteInput", ref deleteInputToggle);





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

                        else if (getObject.Option().Index == optDrawPolyline)
                        {
                            currentState = CommandState.CollectPoints;
                            doc.Objects.UnselectAll();
                            continue;
                        }
                        continue;
                    }

                    else if (getObjResult == GetResult.Cancel)
                    {
                        // Before exiting, save the current settings
                       settings.Save();

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

            // Unsubscribe from event handler
            //  getPoints.DynamicDraw -= dynamicDrawHandler;


            if (inputCurve.IsClosed)
            {
                Curve[] WallPathOffsets = GeometryHelpers.OffsetClosedPolyline(inputCurve, Plane.WorldXY, thicknessOption.CurrentValue, doc.ModelAbsoluteTolerance, selectedAlignment);
                Wall = GeometryHelpers.ExtrudeClosedCurves(WallPathOffsets, Plane.WorldXY, heighOption.CurrentValue, true, selectedAlignment);

            }
            else
            {
                Curve WallPathOffset = GeometryHelpers.OffsetOpenPolyline(inputCurve, Plane.WorldXY, thicknessOption.CurrentValue, doc.ModelAbsoluteTolerance, selectedAlignment);
                Wall = GeometryHelpers.ExtrudeOpenCurve(WallPathOffset, Plane.WorldXY, heighOption.CurrentValue, true).ToBrep();
            }

            // Add created objects to the document
            if (Wall != null) // Check if extrusion succeeded
            {
                Brep wallBrep = Wall; // Convert Extrusion to Brep
                if (wallBrep != null)
                {
                    doc.Objects.AddBrep(wallBrep); // Use AddBrep or Add(Brep)
                    if (deleteInputToggle.CurrentValue && inputObject != null)
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
           settings.Save(); 

            return Result.Success;

        }
    }
}


