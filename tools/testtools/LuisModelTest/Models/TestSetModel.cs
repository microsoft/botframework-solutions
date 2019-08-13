using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using static LuisModelTest.Models.LuisFileModel;

namespace LuisModelTest.Models
{
    public class TestSetModel
    {
        [JsonProperty("utterances")]
        public List<Utterance> Utterances { get; set; }
    }
}
