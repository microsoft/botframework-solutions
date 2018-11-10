using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PointOfInterestSkill.Models
{
    public class DirectionsEventResponse
    {
        public Location Destination { get; set; }

        public RouteDirections.Route Route { get; set; }
    }
}
