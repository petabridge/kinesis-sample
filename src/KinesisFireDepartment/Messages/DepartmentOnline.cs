
namespace KinesisFireDepartment.Messages
{
    public struct DepartmentOnline
    {
        public string Name { get;}   
        public string City { get; }   
        public double Latitude { get;}    
        public double Longitude { get; }
        public DepartmentOnline(string name, string city, double lat, double lg)
        {
            Name = name;
            City = city;    
            Latitude = lat; 
            Longitude = lg; 
        }
        public override string ToString()
        {
            return $"Name:{Name}, City:{City}, Latitude:{Latitude}, Longitude:{Longitude}";
        }
    }
}
