using Newtonsoft.Json;

namespace ToDoSkill
{
    public class TaskItem
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        [JsonProperty(PropertyName = "topic")]
        public string Topic { get; set; }

        [JsonProperty(PropertyName = "isCompleted")]
        public bool IsCompleted { get; set; }
    }
}
