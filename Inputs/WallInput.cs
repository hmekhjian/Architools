using System;
using Architools.Models;
using Rhino.Input.Custom;

namespace Architools.Inputs
{
    public class WallInput
    {
        public OptionDouble HeightOpt;
        public OptionDouble ThicknessOpt;
        public OptionToggle DeleteInputOpt; 
        
        public string[] AlignmentOptions { get; } = new string[] { "Centre", "Interior", "Exterior" };
        public int AlignmentIndex { get; set; } = 0;

        public WallInput(WallSettings settings)
        {
            HeightOpt = new OptionDouble(settings.Height);
            ThicknessOpt = new OptionDouble(settings.Thickness);
            DeleteInputOpt = new OptionToggle(settings.DeleteInput, "False", "True");
            AlignmentIndex = Array.IndexOf(AlignmentOptions, settings.Alignment);
            if (AlignmentIndex == -1) AlignmentIndex = 0;
            
        }
        
        public string CurrentAlignment => AlignmentOptions[AlignmentIndex];

        public void SyncToSettings(WallSettings settings)
        {
            settings.Height = HeightOpt.CurrentValue;
            settings.Thickness = ThicknessOpt.CurrentValue;
            settings.Alignment = CurrentAlignment;
            settings.DeleteInput = DeleteInputOpt.CurrentValue;
        }

    }
}