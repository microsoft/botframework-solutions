using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Bot.Builder.Solutions.Contextual.Services.Sentiment
{
    public class SentimentV3Response
    {
        public IList<DocumentSentiment> Documents { get; set; }

        public IList<ErrorRecord> Errors { get; set; }

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public RequestStatistics Statistics { get; set; }
    }
}
