using Rhino;
using Rhino.Commands;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.Input;
using System;
using System.Collections.Generic;
using Architools.Inputs;
using Architools.Models;
using Architools.Utilities;

namespace Architools.Commands
{
    public class CreateWall : Command
    {
        // public CreateWall()
        // {
        //      Instance = this;
        // }

        ///<summary>The only instance of this command.</summary>
        // public static CreateWall Instance { get; private set; }

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
            WallInput input = new WallInput(settings);
            Curve inputCurve;
            ObjRef inputObject = null;
            Brep wall;

            while (true)
            {
                if (currentState == CommandState.CollectPoints)
                {
                    CustomResult collectionResult = CollectPoints(input, doc, out inputCurve);

                    if (collectionResult == CustomResult.Switch)
                    {
                        currentState = CommandState.SelectCurve;
                        continue;
                    }
                    
                    else if (collectionResult == CustomResult.Finish)
                    {
                        break;
                    }
                    
                    else if (collectionResult == CustomResult.Cancel)
                    {
                        input.SyncToSettings(settings);
                        settings.Save();
                        return Result.Cancel;
                    }
                }

                if (currentState == CommandState.SelectCurve)
                {
                    Rhino.Input.Custom.GetObject getObject = new Rhino.Input.Custom.GetObject();
                    getObject.SetCommandPrompt("Select existing curve for wall path");
                    getObject.GeometryFilter = ObjectType.Curve;
                    getObject.DeselectAllBeforePostSelect = false;
                    getObject.EnablePreSelect(true, true);
                    getObject.ClearCommandOptions();
                    getObject.AddOptionDouble("Height", ref input.HeightOpt);
                    getObject.AddOptionDouble("Thickness", ref input.ThicknessOpt);
                    int optList = getObject.AddOptionList("Alignment", input.AlignmentOptions, input.AlignmentIndex);
                    int optDrawPolyline = getObject.AddOption("DrawPolyline");
                    getObject.AddOptionToggle("DeleteInput", ref input.DeleteInputOpt);





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
                            input.AlignmentIndex = getObject.Option().CurrentListOptionIndex;
                        }

                        else if (getObject.Option().Index == optDrawPolyline)
                        {
                            currentState = CommandState.CollectPoints;
                            doc.Objects.UnselectAll();
                        }
                    }

                    else if (getObjResult == GetResult.Cancel)
                    {
                        // Before exiting, save the current settings
                        input.SyncToSettings(settings);
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

            // Geometry generation
            if (inputCurve.IsClosed)
            {
                Curve[] wallPathOffsets = GeometryHelpers.OffsetClosedPolyline(inputCurve, Plane.WorldXY,
                    input.ThicknessOpt.CurrentValue, doc.ModelAbsoluteTolerance, input.CurrentAlignment);
                wall = GeometryHelpers.ExtrudeClosedCurves(wallPathOffsets, Plane.WorldXY, input.HeightOpt.CurrentValue,
                    true, input.CurrentAlignment);

            }

            else
            {
                Curve wallPathOffset = GeometryHelpers.OffsetOpenPolyline(inputCurve, Plane.WorldXY,
                    input.ThicknessOpt.CurrentValue, doc.ModelAbsoluteTolerance, input.CurrentAlignment);
                wall = GeometryHelpers
                    .ExtrudeOpenCurve(wallPathOffset, Plane.WorldXY, input.HeightOpt.CurrentValue, true).ToBrep();
            }

            // Add created objects to the document
            if (wall != null) // Check if extrusion succeeded
            {
                doc.Objects.AddBrep(wall);
                if (input.DeleteInputOpt.CurrentValue && inputObject != null)
                {
                    RhinoDoc.ActiveDoc.Objects.Delete(inputObject.ObjectId, false);
                }

                doc.Views.Redraw();
            }
            else
            {
                RhinoApp.WriteLine("Failed to create wall extrusion.");
            }

            // Before exiting, save the current settings
            input.SyncToSettings(settings);
            settings.Save();

            return Result.Success;
        }

        private enum CustomResult
        {
            Switch,
            Finish,
            Cancel,
            Failure
        }
            
        CustomResult CollectPoints(WallInput input, RhinoDoc doc, out Curve inputCurve)
        {
            List<Point3d> points = new List<Point3d>();
            Rhino.Input.Custom.GetPoint getPoints = new Rhino.Input.Custom.GetPoint();
            inputCurve = null;
            getPoints.AcceptNothing(true);
            
            EventHandler<Rhino.Input.Custom.GetPointDrawEventArgs> dynamicDrawHandler = (sender, e) =>
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
                    Curve wallPathOffset = GeometryHelpers.OffsetOpenPolyline(previewCurve, Plane.WorldXY, input.ThicknessOpt.CurrentValue, doc.ModelAbsoluteTolerance, input.CurrentAlignment);
                    if (wallPathOffset == null)
                    {
                        return; // Nothing to draw, so exit the handler for this frame.
                    }
                    Brep wallPreview = GeometryHelpers.ExtrudeOpenCurve(wallPathOffset, Plane.WorldXY, input.HeightOpt.CurrentValue, true).ToBrep();

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
            getPoints.DynamicDraw += dynamicDrawHandler;
            
            while (true)
            {
                
                string prompt = (points.Count == 0)
                ? "Select first point of the wall"
                : "Select next point of the wall or press Enter when done";
            
                getPoints.SetCommandPrompt(prompt);
                getPoints.ClearCommandOptions();
                getPoints.AddOptionDouble("Height", ref input.HeightOpt);
                getPoints.AddOptionDouble("Thickness", ref input.ThicknessOpt);
                int optList = getPoints.AddOptionList("Alignment",input.AlignmentOptions, input.AlignmentIndex);
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
                        input.AlignmentIndex = getPoints.Option().CurrentListOptionIndex;
                        continue;
                    }

                    else if (getPoints.Option().Index == optFromCurve)
                    {
                        getPoints.DynamicDraw -= dynamicDrawHandler;
                        return CustomResult.Switch;
                    }
                }
                else if (getResult == GetResult.Nothing)
                {
                    //Check if enough points were selected
                    if (points.Count < 2)
                    {
                        RhinoApp.WriteLine("Not enough points selected to generate wall, at least 2 points are required");
                        continue;
                    }

                    getPoints.DynamicDraw -= dynamicDrawHandler;
                    inputCurve = new PolylineCurve(points);
                    return CustomResult.Finish;

                }
                else if (getResult == GetResult.Cancel)
                {
                    getPoints.DynamicDraw -= dynamicDrawHandler;
                    return CustomResult.Cancel;
                }
                else
                {
                    RhinoApp.WriteLine($"Unexpected input result: {getResult}");
                    getPoints.DynamicDraw -= dynamicDrawHandler;
                    return CustomResult.Failure;
                }
            }
           
        }
        }

    }

