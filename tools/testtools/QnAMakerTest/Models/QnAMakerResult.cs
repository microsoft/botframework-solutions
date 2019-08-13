using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Bot.Builder.AI.QnA;

namespace QnAMakerTest.Models
{
    class QnAMakerResult
    {
        [JsonProperty("answers")]
        public QueryResult[] answers { get; set; }
    }
}
