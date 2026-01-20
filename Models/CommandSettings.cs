using Rhino;

namespace Architools.Models
{
    public abstract class CommandSettings
    {
        private PersistentSettings Settings => ArchitoolsPlugin.Instance.Settings;

        protected double GetDouble(string key, double defaultValue)
        {
            if (Settings.TryGetDouble(key, out double value))
                return value;
            return defaultValue;
        }

        protected double SetDouble(string key, double value)
        {
            Settings.SetDouble(key, value);
            return value;
        }

        protected string GetString(string key, string defaultValue)
        {
            if (Settings.TryGetString(key, out string value))
                return value;
            return defaultValue;
        }

        protected string SetString(string key, string value)
        {
            Settings.SetString(key, value);
            return value;
        }

        protected bool GetBool(string key, bool defaultValue)
        {
            if (Settings.TryGetBool(key, out bool value))
                return value;
            return defaultValue;
        }

        protected bool SetBool(string key, bool value)
        {
            Settings.SetBool(key, value);
            return value;
        }

        public abstract void Load(RhinoDoc doc);
        public abstract void Save();
    }

    public class WallSettings : CommandSettings
    {
        private const string KEY_HEIGHT = "Wall_Height";
        private const string KEY_THICKNESS = "Wall_Thickness";
        private const string KEY_ALIGNMENT = "Wall_Alignment";
        private const string KEY_INPUT = "Wall_DeleteInput";

        public double Height {get; set; } = 3000;
        public double Thickness {get; set; } = 300;
        public string Alignment {get; set; } = "Centre";
        public bool DeleteInput {get; set; } = false;
        

        public override void Load(RhinoDoc doc)
        {
            Height = PluginUtils.ConvertToDocumentUnits(doc, Height);
            Thickness = PluginUtils.ConvertToDocumentUnits(doc, Thickness);
            
            Height = GetDouble(KEY_HEIGHT, Height);
            Thickness = GetDouble(KEY_THICKNESS, Thickness);
            Alignment = GetString(KEY_ALIGNMENT, Alignment);
            DeleteInput = GetBool(KEY_INPUT, DeleteInput);
        }

        public override void Save()
        {
            SetDouble(KEY_HEIGHT, Height);
            SetDouble(KEY_THICKNESS, Thickness);
            SetString(KEY_ALIGNMENT, Alignment);
            SetBool(KEY_INPUT, DeleteInput);
        }


    }
}
    

    