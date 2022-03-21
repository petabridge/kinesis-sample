namespace Shared
{
    public class SensorSetting
    {
        public string SensorId { get; } = Guid.NewGuid().ToString();
        public string SensorName { get; set; }
        public double Longitude { get; set; }
        public double Latitude { get; set; }
    }
}
