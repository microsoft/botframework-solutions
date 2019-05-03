using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WeatherSkill.Models
{
    public class Geoposition
    {
        public float Latitude { get; set; }

        public float Longitude { get; set; }

        public Elevation Elevation { get; set; }
    }
}
