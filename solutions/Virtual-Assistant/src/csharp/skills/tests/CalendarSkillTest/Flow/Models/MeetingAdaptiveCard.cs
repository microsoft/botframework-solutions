using Newtonsoft.Json;

namespace CalendarSkillTest.Flow.Models
{
    public class MeetingAdaptiveCard
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("version")]
        public string Version { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("speak")]
        public string Speak { get; set; }

        [JsonProperty("body")]
        public Body[] Bodies { get; set; }

        public class Body
        {
            [JsonProperty("type")]
            public string Type { get; set; }

            [JsonProperty("items")]
            public Item[] Items { get; set; }
        }

        public class Item
        {
            [JsonProperty("type")]
            public string Type { get; set; }

            [JsonProperty("size")]
            public string Size { get; set; }

            [JsonProperty("weight")]
            public string Weight { get; set; }

            [JsonProperty("text")]
            public string Text { get; set; }

            [JsonProperty("maxLines")]
            public int MaxLines { get; set; }
        }
    }
}