using System;
using System.Collections.Generic;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Microsoft.Bot.Builder.Solutions.Contextual.Services
{
    public class SentimentService
    {
        public static async Task<DocumentSentimentLabel> GetSentimentAnalysisAsync(string utterance, string locale)
        {
            var inputDocuments = new TextAnalyticsBatchInput()
            {
                Documents = new List<TextAnalyticsInput>()
                        {
                            new TextAnalyticsInput()
                            {
                                Id = "1",
                                Text = utterance,
                                LanguageCode = locale
                            },
                        },
            };

            var sentimentV3Prediction = TextAnalyticsSentimentV3Client.SentimentV3PreviewPredictAsync(inputDocuments).Result;
            return sentimentV3Prediction.Documents[0].Sentiment;
        }
    }

    public class TextAnalyticsSentimentV3Client
    {
        //You can get the reqeust url by going to: 
        //https://centralus.dev.cognitive.microsoft.com/docs/services/TextAnalytics-v3-0-preview/operations/56f30ceeeda5650db055a3c9
        //and clicking on the region (e.g. Central US). 
        private static readonly string textAnalyticsUrl = "https://centralus.api.cognitive.microsoft.com/text/analytics/v3.0-preview/sentiment";
        private static readonly string textAnalyticsKey = "3a93126e3e9142f9adc9cf2bc74ffc6c";

        public static async Task<SentimentV3Response> SentimentV3PreviewPredictAsync(TextAnalyticsBatchInput inputDocuments)
        {
            using (var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", textAnalyticsKey);

                var httpContent = new StringContent(JsonConvert.SerializeObject(inputDocuments), Encoding.UTF8, "application/json");

                var httpResponse = await httpClient.PostAsync(new Uri(textAnalyticsUrl), httpContent);
                var responseContent = await httpResponse.Content.ReadAsStringAsync();

                if (!httpResponse.StatusCode.Equals(HttpStatusCode.OK) || httpResponse.Content == null)
                {
                    throw new Exception(responseContent);
                }

                return JsonConvert.DeserializeObject<SentimentV3Response>(responseContent, new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore });
            }
        }
    }

    //public static void Main(string[] args)
    //{
    //    var inputDocuments = new TextAnalyticsBatchInput()
    //    {
    //        Documents = new List<TextAnalyticsInput>()
    //            {
    //                new TextAnalyticsInput()
    //                {
    //                    Id = "1",

    //                    Text = "Hello world. This is some input text that I love."
    //                },

    //                new TextAnalyticsInput()
    //                {
    //                    Id = "2",

    //                    Text = "It's incredibly sunny outside! I'm so happy."
    //                },

    //                new TextAnalyticsInput()
    //                {
    //                    Id = "3",

    //                    Text = "Pike place market is my favorite Seattle attraction."
    //                }
    //            }
    //    };
    //    //If you’re using C# 7.1 or greater, you can use an async main() method to await the function
    //    var sentimentV3Prediction = TextAnalyticsSentimentV3Client.SentimentV3PreviewPredictAsync(inputDocuments).Result;

    //    // Replace with whatever you wish to print or simply consume the sentiment v3 prediction
    //    Console.WriteLine("Document ID=" + sentimentV3Prediction.Documents[0].Id + " : Sentiment=" + sentimentV3Prediction.Documents[0].Sentiment);
    //    Console.WriteLine("Document ID=" + sentimentV3Prediction.Documents[1].Id + " : Sentiment=" + sentimentV3Prediction.Documents[1].Sentiment);
    //    Console.WriteLine("Document ID=" + sentimentV3Prediction.Documents[2].Id + " : Sentiment=" + sentimentV3Prediction.Documents[2].Sentiment);
    //    Console.ReadKey();
    //}

    public class SentimentV3Response
    {
        public IList<DocumentSentiment> Documents { get; set; }

        public IList<ErrorRecord> Errors { get; set; }

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public RequestStatistics Statistics { get; set; }
    }

    public class TextAnalyticsBatchInput
    {
        public IList<TextAnalyticsInput> Documents { get; set; }
    }

    public class TextAnalyticsInput
    {
        /// <summary>
        /// A unique, non-empty document identifier.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// The input text to process.
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// The language code. Default is english ("en").
        /// </summary>
        public string LanguageCode { get; set; } = "en";
    }

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

    public enum DocumentSentimentLabel
    {
        Positive,

        Neutral,

        Negative,

        Mixed,
    }

    public enum SentenceSentimentLabel
    {
        Positive,

        Neutral,

        Negative,
    }

    public class SentimentConfidenceScoreLabel
    {
        public double Positive { get; set; }

        public double Neutral { get; set; }

        public double Negative { get; set; }
    }

    public class ErrorRecord
    {
        /// <summary>
        /// The input document unique identifier that this error refers to.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// The actual error message.
        /// </summary>
        public string Message { get; set; }
    }

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

    public class RequestStatistics
    {
        /// <summary>
        /// Number of documents submitted in the request.
        /// </summary>
        public int DocumentsCount { get; set; }

        /// <summary>
        /// Number of valid documents. This excludes empty, over-size limit or non-supported languages documents.
        /// </summary>
        public int ValidDocumentsCount { get; set; }

        /// <summary>
        /// Number of invalid documents. This includes empty, over-size limit or non-supported languages documents.
        /// </summary>
        public int ErroneousDocumentsCount { get; set; }

        /// <summary>
        /// Number of transactions for the request.
        /// </summary>
        public long TransactionsCount { get; set; }
    }
}
