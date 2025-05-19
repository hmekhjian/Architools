using System;
using System.Security.Cryptography.X509Certificates;
using System.Collections.Generic;
using Rhino;
using Rhino.Input;
using Rhino.Commands;
using Rhino.Geometry;

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


            // Define command option types
            Rhino.Input.Custom.OptionDouble heighOption = new Rhino.Input.Custom.OptionDouble(3000, 0, 10000);
            Rhino.Input.Custom.OptionDouble thicknessOption = new Rhino.Input.Custom.OptionDouble(300, 0, 1000);
            string[] listValues = new string[] { "Centre", "Interior", "Exterior" };
            int listIndex = 0;

            Rhino.Input.Custom.GetPoint getPoints = new Rhino.Input.Custom.GetPoint();
            getPoints.AcceptNothing(true);

            List<Point3d> points = new List<Point3d>();
            string selectedAlignment = listValues[0];
            bool useExistingCurve = false;
            Curve inputCurve = null;


            RhinoApp.WriteLine($"Debug RunCommand: Initial selectedAlignment: '{selectedAlignment}'");


            //While loop to collect points for the creation of the wall
            while (true)
            {
                string prompt = (points.Count == 0)
                    ? "Select first point of the wall"
                    : "Select next point of the wall or press Enter when done";
                getPoints.SetCommandPrompt(prompt);

                //Add options to the command instance
                getPoints.ClearCommandOptions();
                getPoints.AddOptionDouble("Height", ref heighOption);
                getPoints.AddOptionDouble("Thickness", ref thicknessOption);
                int optList = getPoints.AddOptionList("Alignment", listValues, listIndex);
                int optFromCurve = getPoints.AddOption("From Curve");

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
                        RhinoApp.WriteLine($"Debug RunCommand: selectedAlignment updated to '{selectedAlignment}' after option selection.");
                        continue;
                    }

                    else if (getPoints.Option().Index == optFromCurve)
                    {
                        useExistingCurve = true;
                        break
                    }
                }
                else if (getResult == GetResult.Nothing)
                {
                    RhinoApp.WriteLine($"Debug RunCommand: GetResult.Nothing received. Breaking loop. Final selectedAlignment before break: '{selectedAlignment}'");
                    break;
                }
                else if (getResult == GetResult.Cancel)
                {
                    RhinoApp.WriteLine("Command was cancelled");
                    return Result.Cancel;
                }
                else
                {
                    RhinoApp.WriteLine($"Unexpected input result: {getResult}");
                    return Result.Failure;
                }
                }

            RhinoApp.WriteLine($"Debug RunCommand: Loop broken. selectedAlignment before calling OffsetPolyline: '{selectedAlignment}'");
            //Check if enough points were selected

            if (selectedAlignment)
            {
                Rhino.Input.Custom.GetObject getObject = new Rhino.Input.Custom.GetObject();
                getObject.SetCommandPrompt("Select existing curve for wall path");
                getObject.GeometryFilter = Rhino.DocObjects.ObjectType.Curve;
                getObject.DeselectAllBeforePostSelect = false;
                getObject.EnablePreSelect(true, true);

                GetResult getObjResult = getObject.Get();

                // TODO Add the result logic by adding 

                if (getObjResult == GetResult.Object)
                {
                    inputCurve = getObject.Object(0).Curve();
                }

                else if (getObjResult == getObject.Option())

            }

            else
            {

                if (points.Count < 2)
                {
                    RhinoApp.WriteLine("Not enough points selected to generate wall, at least 2 points are required");
                    return Result.Failure;
                }

                PolylineCurve WallPath = new PolylineCurve(points);
                Curve WallPathOffset = GeometryHelpers.OffsetPolyline(WallPath, Plane.WorldXY, thicknessOption.CurrentValue, 0.1, selectedAlignment);
                Extrusion Wall = GeometryHelpers.ExtrudeCurve(WallPathOffset, Plane.WorldXY, heighOption.CurrentValue, true);

                // Add created objects to the document
                if (Wall != null) // Check if extrusion succeeded
                {
                    Brep wallBrep = Wall.ToBrep(); // Convert Extrusion to Brep
                    if (wallBrep != null)
                    {
                        doc.Objects.AddBrep(wallBrep); // Use AddBrep or Add(Brep)
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


                return Result.Success;
            }
        }
    }
            }


