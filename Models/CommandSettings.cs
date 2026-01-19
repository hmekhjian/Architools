namespace Architools.Models
{
    public abstract class CommandSettings
    {
        protected double GetDouble(string key, double defaultValue)
        {
            var settings = ArchitoolsPlugin.Instance.Settings;
            if (settings.TryGetDouble(key, out double value))
                return value;
            return defaultValue;
        }

        protected double SetDouble(string key, double value)
        {
            
        }
        
    }

    public class WallSettings : CommandSettings
    {


    }clear
    

    public class RailSettings : CommandSettings
    {
        
    }
}