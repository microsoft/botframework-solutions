namespace PointOfInterestSkill.Models
{
    public class DirectionsEventResponse
    {
        public Location Destination { get; set; }

        public RouteDirections.Route Route { get; set; }
    }
}