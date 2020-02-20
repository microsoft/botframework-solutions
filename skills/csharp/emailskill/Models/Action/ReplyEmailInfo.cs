using Newtonsoft.Json;

namespace EmailSkill.Models.Action
{
    public class ReplyEmailInfo
    {
        [JsonProperty("replyMessage")]
        public string ReplyMessage { get; set; }
    }
}
