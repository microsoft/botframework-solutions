namespace ITSMSkill.Extensions.Teams.TaskModule
{
    using Newtonsoft.Json;

    public class AdaptiveCardValue<T>
    {
        [JsonProperty("msteams")]
        public object Type { get; set; } = JsonConvert.DeserializeObject("{\"type\": \"task/fetch\" }");

        [JsonProperty("data")]
        public T Data { get; set; }
    }
}
