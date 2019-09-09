using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Bot.Builder.Solutions.Contextual.Services.Sentiment
{
    public class SentimentConfidenceScoreLabel
    {
        public double Positive { get; set; }

        public double Neutral { get; set; }

        public double Negative { get; set; }
    }
}
