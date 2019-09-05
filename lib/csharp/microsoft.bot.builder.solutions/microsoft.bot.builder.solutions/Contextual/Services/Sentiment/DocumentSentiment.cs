using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Bot.Builder.Solutions.Contextual.Services.Sentiment
{
    public class DocumentSentiment
    {
        public DocumentSentiment(
            string id,
            DocumentSentimentLabel sentiment,
            SentimentConfidenceScoreLabel documentSentimentScores,
            IEnumerable<SentenceSentiment> sentencesSentiment)
        {
            Id = id;

            Sentiment = sentiment;

            DocumentScores = documentSentimentScores;

            Sentences = sentencesSentiment;
        }

        /// <summary>
        /// A unique, non-empty document identifier.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Predicted sentiment for document (Negative, Neutral, Positive, or Mixed).
        /// </summary>
        public DocumentSentimentLabel Sentiment { get; set; }

        /// <summary>
        /// Document level sentiment confidence scores for each sentiment class.
        /// </summary>
        public SentimentConfidenceScoreLabel DocumentScores { get; set; }

        /// <summary>
        /// Sentence level sentiment analysis.
        /// </summary>
        public IEnumerable<SentenceSentiment> Sentences { get; set; }
    }
}
