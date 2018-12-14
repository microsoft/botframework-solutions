using System;
using System.Collections.Generic;
using System.Text;

namespace CalendarSkillTest.Flow.Models
{
    public class MeetingAdaptiveCard
    {
        public string type { get; set; }

        public string version { get; set; }

        public string id { get; set; }

        public string speak { get; set; }

        public Body[] body { get; set; }

        public class Body
        {
            public string type { get; set; }

            public Item[] items { get; set; }
        }

        public class Item
        {
            public string type { get; set; }

            public string size { get; set; }

            public string weight { get; set; }

            public string text { get; set; }

            public int maxLines { get; set; }
        }
    }
}