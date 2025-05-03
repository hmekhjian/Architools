using System;
using Rhino;
using Rhino.Commands;

namespace Architools
{
    public class Wall : Command
    {
        public Wall()
        {
            Instance = this;
        }

        ///<summary>The only instance of this command.</summary>
        public static Wall Instance { get; private set; }

        public override string EnglishName => "Wall";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            // TODO: complete command.
            return Result.Success;
        }
    }
}