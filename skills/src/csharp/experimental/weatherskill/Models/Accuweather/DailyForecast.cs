using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WeatherSkill.Models
{
    public class DailyForecast
    {
        public DateTime Date { get; set; }

        public int EpochDate { get; set; }

        public Temperature Temperature { get; set; }

        public Day Day { get; set; }

        public Night Night { get; set; }

        public string[] Sources { get; set; }

        public string MobileLink { get; set; }

        public string Link { get; set; }
    }
}
