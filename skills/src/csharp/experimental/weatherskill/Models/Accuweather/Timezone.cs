using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WeatherSkill.Models
{
    public class Timezone
    {
        public string Code { get; set; }

        public string Name { get; set; }

        public float GmtOffset { get; set; }

        public bool IsDaylightSaving { get; set; }

        public DateTime NextOffsetChange { get; set; }
    }
}
