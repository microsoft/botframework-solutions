using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace VirtualAssistantSample.Models
{
    // Skill Card action data should contain skillName parameter
    // This class is used to deserialize it and get skillName
    public class SkillCardActionData
    {
        /// <summary>
        /// Gets skillName
        /// </summary>  
        [JsonProperty("AppId")]
        public string SkillId { get; set; }
    }
}
