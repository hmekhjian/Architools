using System;
using System.Collections.Generic;
using Rhino;
using Rhino.Commands;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;

namespace Architools
{
    public class CreateRailing : Command
    {
        public CreateRailing()
        {
            Instance = this;
        }

        ///<summary>The only instance of this command.</summary>
        public static CreateRailing Instance { get; private set; }

        public override string EnglishName => "CreateRailing";

        private enum CommandState
        {
            CollectPoints,
            SelectCurve
        }

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            CommandState currentState = CommandState.CollectPoints;
            double height = 1100;
            double thickness = 100;

            string alignment = "Centre";
            bool deleteInput = false;


            Rhino.Input.Custom.OptionDouble heightOption = new Rhino.Input.Custom.OptionDouble(height);
            Rhino.Input.Custom.OptionDouble thicknessOption = new Rhino.Input.Custom.OptionDouble(thickness);
            string[] listValues = new string[] { "Centre", "Interior", "Exterior" };
            int listIndex = Array.IndexOf(listValues, alignment);
            if (listIndex == -1) listIndex = 0;
            Rhino.Input.Custom.OptionToggle deleteInputToggle = new Rhino.Input.Custom.OptionToggle(deleteInput, "False", "True");

            Rhino.Input.Custom.GetPoint getPoints = new Rhino.Input.Custom.GetPoint();
            getPoints.AcceptNothing(true);

            List<Point3d> points = new List<Point3d>();
            string selectedAlingment = listValues[0];
            bool useExistingCurve = false;
            ObjRef inputObject = null;
            Curve inputCurve = null;
            Brep Railing = null;

            // Event handler for dynamicly previewing content will go in here once I decide what the preview of this command needs to be
            while (true)
            {
                if (currentState == CommandState.CollectPoints)
                {
                    string prompt = (points.Count == 0)
                        ? "Select first point of the railing"
                        : "Select next point of railing or press Enter when done";
                    getPoints.SetCommandPrompt(prompt);
                    getPoints.ClearCommandOptions();
                    getPoints.AddOptionDouble("Height", ref heightOption);
                    getPoints.AddOptionDouble("Thickness", ref thicknessOption);
                    int optList = getPoints.AddOptionList("Alignment", listValues, listIndex);
                    int optFromCurve = getPoints.AddOption("FromCurve");

                    // Dynamic draw handler subscriptions should go in here in there future once we decide what the preview is going to be
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
                            selectedAlingment = listValues[getPoints.Option().CurrentListOptionIndex];
                            listIndex = getPoints.Option().CurrentListOptionIndex;
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
                        if (points.Count < 2)
                        {
                            RhinoApp.WriteLine("Not enough points selected to generate a railing, at least 2 points are rquired");
                            return Result.Failure;
                        }

                        inputCurve = new PolylineCurve(points);
                        break;


                    }

                    else if (getResult == GetResult.Cancel)
                    {
                        // Command options settings to be stored here once implemented 

                        RhinoApp.WriteLine("Command was cancelled");
                        return Result.Cancel;
                    }

                    else
                    {
                        RhinoApp.WriteLine($"Unexpecteed input result: {getResult}");
                        return Result.Failure;
                    }
                }

                if (currentState == CommandState.SelectCurve)
                {
                    return Result.Failure;
                }


                // TODO: complete command.
                return Result.Success;
            }
        }
    }
}