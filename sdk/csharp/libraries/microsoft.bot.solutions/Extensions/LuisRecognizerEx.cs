using System;
using System.Collections.Generic;
using Microsoft.Azure.CognitiveServices.Language.LUIS.Runtime.Models;
using Microsoft.Bot.Solutions.Models;
using Newtonsoft.Json;

namespace Microsoft.Bot.Solutions.Extensions
{
    public static class LuisRecognizerEx
    {
        private const string _sentiment = "sentiment";
        private const string _postive_sentiment = "positive";
        private const string _neutral_sentiment = "neutral";
        private const string _negative_sentiment = "negative";

        public static (SentimentType label, double score) GetSentimentInfo<T>(this T luisConverter, Func<T, IDictionary<string, object>> propertyAccessor)
        {
            SentimentType sentimentLabel = SentimentType.None;
            double maxScore = 0.0;

            var luisProperty = propertyAccessor(luisConverter);

            if (luisProperty != null && luisProperty.TryGetValue(_sentiment, out var result))
            {
                var sentimentInfo = JsonConvert.DeserializeObject<Sentiment>(result.ToString());
                sentimentLabel = GetSentimentType(sentimentInfo.Label);
                maxScore = sentimentInfo.Score.HasValue ? sentimentInfo.Score.Value : 0.0;
            }

            return (sentimentLabel, maxScore);
        }

        public static SentimentType GetSentimentType(string label)
        {
            var sentimentType = SentimentType.None;

            if (string.Equals(label, _postive_sentiment))
            {
                sentimentType = SentimentType.Positive;
            }
            else if (string.Equals(label, _neutral_sentiment))
            {
                sentimentType = SentimentType.Neutral;
            }
            else if (string.Equals(label, _negative_sentiment))
            {
                sentimentType = SentimentType.Negative;
            }

            return sentimentType;
        }
    }
}
