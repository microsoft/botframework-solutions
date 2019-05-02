using Microsoft.Bot.Builder.Solutions.Responses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WeatherSkill.Models
{
    public class HourDetails
    {
        public string Icon { get; set; }

        public int Temperature { get; set; }

        public string Hour { get; set;}
    }
}
