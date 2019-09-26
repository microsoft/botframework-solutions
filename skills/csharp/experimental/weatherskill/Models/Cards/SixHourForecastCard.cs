using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Solutions.Responses;

namespace WeatherSkill.Models
{
    public class SixHourForecastCard : ICardData
    {
        public string Location { get; set; }

        public string Date { get; set; }

        public int MinimumTemperature { get; set; }

        public int MaximumTemperature { get; set; }

        public string ShortPhrase { get; set; }

        public string WindDescription { get; set; }

        public string DayIcon { get; set; }

        public string Speak { get; set; }

        public string Hour1 { get; set; }

        public string Icon1 { get; set; }

        public int Temperature1 { get; set; }

        public string Hour2 { get; set; }

        public string Icon2 { get; set; }

        public int Temperature2 { get; set; }

        public string Hour3 { get; set; }

        public string Icon3 { get; set; }

        public int Temperature3 { get; set; }

        public string Hour4 { get; set; }

        public string Icon4 { get; set; }

        public int Temperature4 { get; set; }

        public string Hour5 { get; set; }

        public string Icon5 { get; set; }

        public int Temperature5 { get; set; }

        public string Hour6 { get; set; }

        public string Icon6 { get; set; }

        public int Temperature6 { get; set; }
    }
}
