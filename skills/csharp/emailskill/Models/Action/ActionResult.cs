using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EmailSkill.Models.Action
{
    public class ActionResult
    {
        [JsonProperty("actionSuccess")]
        public bool ActionSuccess { get; set; }
    }
}
