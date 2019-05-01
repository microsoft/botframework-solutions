using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WeatherSkill.Models
{
    public class Temperature
    {
        public float Value { get; set; }
        public string Unit { get; set; }
        public int UnitType { get; set; }

        public Minimum Minimum { get; set; }
        public Maximum Maximum { get; set; }
    }
}
