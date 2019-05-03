using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WeatherSkill.Models
{
    public class HourlyForecast
    {
        public DateTime DateTime { get; set; }

        public int EpochDateTime { get; set; }

        public int WeatherIcon { get; set; }

        public string IconPhrase { get; set; }

        public bool IsDaylight { get; set; }

        public Temperature Temperature { get; set; }

        public int PrecipitationProbability { get; set; }

        public string MobileLink { get; set; }

        public string Link { get; set; }
    }
}
