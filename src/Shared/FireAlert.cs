
namespace Shared
{
    public struct FireAlert
    {
        public Coordinate Location { get; }
        public int Reading { get; } 
        public FireAlertType Level { get; }
        public FireAlert(Coordinate location, int reading, FireAlertType level)
        {
            Location = location;
            Reading = reading;
            Level = level;
        }
        public override string ToString()
        {
            return $"Location:({Location.Latitude},{Location.Longitude}), Reading:({Reading}), Alert Level:({Level})";
        }
    }
    public enum FireAlertType
    {
        High,
        Low,
        Normal,
        VeryHigh,
        ExtremeHigh
    }
}
