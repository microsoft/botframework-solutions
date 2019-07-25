using Newtonsoft.Json;
using System;

namespace ToDoSkill.Models
{
    public class TaskItem
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        [JsonProperty(PropertyName = "topic")]
        public string Topic { get; set; }

        [JsonProperty(PropertyName = "isCompleted")]
        public bool IsCompleted { get; set; }

        public DateTime ReminderDateTime { get; set; }
    }
}