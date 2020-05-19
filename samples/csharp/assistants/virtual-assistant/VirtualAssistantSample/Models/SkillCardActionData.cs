using Newtonsoft.Json;

namespace VirtualAssistantSample.Models
{
    public class SkillCardActionData
    {
        /// <summary>
        /// Gets or sets skillName
        /// </summary>
        /// <value>
        /// return SkillName
        /// </value>
        [JsonProperty("AppName")]
        public string SkillName { get; set; }
    }
}
