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
                int listIndex = 0;
                int optList = getPoints.AddOptionList("Alignment", listValues, listIndex);

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
                else {
                    RhinoApp.WriteLine($"Unexpected input result: {getResult}");
                    return Result.Failure;
            }
            
            



            return Result.Success;
        }
    }
}