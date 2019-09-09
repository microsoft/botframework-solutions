using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Microsoft.Bot.Builder.Solutions.Contextual.Services.Sentiment;

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
}
