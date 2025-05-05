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


            //While loop to collect points for the creation of the wall
            while (true)
            {
                string prompt = (points.Count == 0)
                    ? "Select first point of the wall"
                    : "Select next point of the wall or press Enter when done";
                getPoints.SetCommandPrompt(prompt);

                //Add options to the command instance
                getPoints.AddOptionDouble("Height", ref heighOption);
                getPoints.AddOptionDouble("Thichkness", ref thicknessOption);
                int optList = getPoints.AddOptionList("Alignment", listValues, ref listIndex);

                GetResult getResult = getPoints.Get();

                if (getResult == GetResult.Point)
                {
                    points.Add(getPoints.Point());
                    doc.Views.Redraw();
                    continue;
                }
                else if (getResult == GetResult.Option)
                {
                    continue;
                }
                else if (getResult == GetResult.Nothing)
                {
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

             //Check if enough points were selected

            if (points.Count < 2)
            {
                RhinoApp.WriteLine("Not enough points selected to generate wall, at least 2 points are required");
                return Result.Failure;
            }
          
            PolylineCurve WallPath = new PolylineCurve(points);
            Curve WallPathOffset = GeometryHelpers.OffsetPolyline(WallPath, Plane.WorldXY, thicknessOption.CurrentValue, 0.1);
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