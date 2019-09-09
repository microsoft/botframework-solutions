using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Bot.Builder.Solutions.Contextual.Services.Sentiment
{
    public class SentenceSentiment
    {
        /// <summary>
        /// The predicted Sentiment for the sentence.
        /// </summary>
        public SentenceSentimentLabel Sentiment { get; set; }

        /// <summary>
        /// The sentiment confidence score for the sentence for all classes.
        /// </summary>
        public SentimentConfidenceScoreLabel SentenceScores { get; set; }

        /// <summary>
        /// The sentence offset from the start of the document.
        /// </summary>
        public int Offset { get; set; }

        /// <summary>
        /// The sentence length as given by StringInfo's LengthInTextElements property.
        /// </summary>
        public int Length { get; set; }

        /// <summary>
        /// The warnings generated for the sentence.
        /// </summary>
        public string[] Warnings { get; set; }
    }
}
