namespace PointOfInterestSkill.Models
{
    public class DirectionsEventResponse
    {
        public PointOfInterestModel Destination { get; set; }

        public RouteDirections.Route Route { get; set; }
    }
}