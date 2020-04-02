namespace ITSMSkill.Extensions.Teams.TaskModule
{
    using Newtonsoft.Json;

    public class TaskProperty
    {
        [JsonProperty("value")]
        public TaskInfo TaskInfo { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }
    }
}
