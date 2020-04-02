namespace ITSMSkill.Extensions.Teams.TaskModule
{
    using Newtonsoft.Json;

    public class TaskEnvelope : ITeamsInvokeEnvelope
    {
        [JsonProperty("task")]
        public TaskProperty Task { get; set; }
    }
}
