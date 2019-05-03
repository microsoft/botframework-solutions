using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WeatherSkill.Models
{
    public class Day
    {
        public int Icon { get; set; }

        public string IconPhrase { get; set; }

        public string ShortPhrase { get; set; }

        public string LongPhrase { get; set; }

        public Wind Wind { get; set; }
    }
}
