using Newtonsoft.Json;
using System.Collections.Generic;

namespace EmailSkill.Models.Action
{
    public class EmailInfo
    {
        [JsonProperty("subject")]
        public string Subject { get; set; }

        [JsonProperty("content")]
        public string Content { get; set; }

        [JsonProperty("sender")]
        public string Sender { get; set; }

        [JsonProperty("reciever")]
        public List<string> Reciever { get; set; }
    }
}