using System;
using Rhino;
using Rhino.Commands;

namespace Architools
{
    public class Railing : Command
    {
        public Railing()
        {
            Instance = this;
        }

        ///<summary>The only instance of this command.</summary>
        public static Railing Instance { get; private set; }

        public override string EnglishName => "CreateRailing";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            // TODO: complete command.
            return Result.Success;
        }
    }
}