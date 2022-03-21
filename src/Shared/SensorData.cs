using System;

namespace Shared
{
    public class SensorData
    {
        public string SensorId { get; set; }
        public string SensorName { get; set; }
        public int Reading { get; set; }
        public Coordinate Coordinate { get; set; }
    }
}
