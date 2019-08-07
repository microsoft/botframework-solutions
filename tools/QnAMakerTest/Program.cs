using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using QnAMakerTest.Models;
using Microsoft.Bot.Builder.AI.QnA;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace QnAMakerTest
{
    class Program
    {
        static HttpClient client;
        static void Main(string[] args)
        {
            var qnamakerManager = new QnAmakerManager.QnAMakerManager("knowledgebases_settings.json");
            var qnamakerService = qnamakerManager.QnAMakerService;
            client = new HttpClient();

            bool replaced = ReplaceQnAMakerKB(qnamakerService.kbID, qnamakerService.subscriptionKey, qnamakerService.trainFileName).Result;
            if(replaced)
            {
                bool published = PublishQnAMakerKB(qnamakerService.kbID, qnamakerService.subscriptionKey).Result;
                if(published)
                {
                    RunTest(qnamakerService.kbID, qnamakerService.endpointKey, qnamakerService.hostname, qnamakerService.testFileName).Wait();
                }
            }
            Console.WriteLine("Finished. Press any key to exit");
            Console.ReadKey();
        }

        private static async Task<bool> ReplaceQnAMakerKB(string kbId, string subscriptionKey, string trainDataPath)
        {
            Console.WriteLine("Replacing QnAMaker KB...");
            StreamReader sr = new StreamReader(trainDataPath);
            string json = sr.ReadToEnd();
            sr.Close();
            QnAFileModel qnaFileModel = JsonConvert.DeserializeObject<QnAFileModel>(json);

            //var client = new HttpClient();
            var uri = "https://westus.api.cognitive.microsoft.com/qnamaker/v4.0/knowledgebases/" + kbId;
            HttpRequestMessage request = new HttpRequestMessage();
            request.Method = HttpMethod.Put;
            request.RequestUri = new Uri(uri);
            request.Headers.Add("Ocp-Apim-Subscription-Key", subscriptionKey);
            JObject obj = JObject.FromObject(new { qnalist = qnaFileModel.QnAList });
            string body = JsonConvert.SerializeObject(obj, Formatting.Indented);
            request.Content = new StringContent(body, Encoding.UTF8, "application/json");

            HttpResponseMessage response = await client.SendAsync(request);
            if(response.IsSuccessStatusCode)
            {
                Console.WriteLine("Replace finished.");
                return true;
            }
            else
            {
                Console.WriteLine("Replace failed!");
                return false;
            }
        }

        private static async Task<bool> PublishQnAMakerKB(string kbId, string subscriptionKey)
        {
            Console.WriteLine("Publishing QnAMaker KB...");
            //var client = new HttpClient();
            var uri = "https://westus.api.cognitive.microsoft.com/qnamaker/v4.0/knowledgebases/" + kbId;
            HttpRequestMessage request = new HttpRequestMessage();
            request.Method = HttpMethod.Post;
            request.RequestUri = new Uri(uri);
            request.Headers.Add("Ocp-Apim-Subscription-Key", subscriptionKey);

            HttpResponseMessage response = await client.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine("Publish finished.");
                return true;
            }
            else
            {
                Console.WriteLine("Publish failed.");
                return false;
            }
        }

        private static async Task RunTest(string knowledgeBaseId, string endpointKey, string host, string fileName)
        {
            var qnaEndpoint = new QnAMakerEndpoint()
            {
                KnowledgeBaseId = knowledgeBaseId,
                EndpointKey = endpointKey,
                Host = host,
            };
            
            // input file
            Console.WriteLine("Reading test set...");

            StreamReader  sr = new StreamReader(fileName);
            string json = sr.ReadToEnd();
            sr.Close();
            QnAFileModel qnaFileModel= JsonConvert.DeserializeObject<QnAFileModel>(json);

            
            Console.WriteLine("Start testing...");
            StreamWriter output = new StreamWriter(fileName + "_error_case.json");
            int passed = 0;
            int total = 0;
            foreach (QnAFileModel.QnADTO qna in qnaFileModel.QnAList)
            {
                foreach(string question in qna.Questions)
                {
                    QnAMakerResult qnaMakerResult = await GetQnAResult(qnaEndpoint, question);
                    if (qnaMakerResult.answers.Length > 0 && string.Compare(qnaMakerResult.answers[0].Answer, 0, qna.Answer, 0, 50, true) == 0)
                        passed++;
                    else
                    {
                        output.WriteLine(question);
                        output.WriteLine("QnAMaker Result:");
                        output.WriteLine(qnaMakerResult.answers[0].Answer);
                        output.WriteLine("Expected:");
                        output.WriteLine(qna.Answer);
                        output.WriteLine("");
                        Console.WriteLine(question);
                    }
                    total++;
                }
            }
            output.Flush();
            output.Close();
            Console.WriteLine("Total: " + total);
            Console.WriteLine("Passed: " + passed);
        }


        private static async Task<QnAMakerResult> GetQnAResult(QnAMakerEndpoint qnaEndpoint, string question)
        {
            //var client = new HttpClient();
            var request = BuildRequest(qnaEndpoint, question);
            HttpResponseMessage response = await client.SendAsync(request).ConfigureAwait(false);
            //response.EnsureSuccessStatusCode();
            string result =  await response.Content.ReadAsStringAsync();
            QnAMakerResult qr = JsonConvert.DeserializeObject<QnAMakerResult>(result);
            return JsonConvert.DeserializeObject<QnAMakerResult>(result);
        }
        private static HttpRequestMessage BuildRequest(QnAMakerEndpoint qnaEndpoint, string query)
        {
            var requestUrl = $"{qnaEndpoint.Host}/qnamaker/knowledgebases/{qnaEndpoint.KnowledgeBaseId}/generateanswer";
            var request = new HttpRequestMessage(HttpMethod.Post, requestUrl);

            var options = new QnAMakerOptions();
            ValidateOptions(options);

            var jsonRequest = JsonConvert.SerializeObject(
                new
                {
                    question = query,
                    top = 1
                });
            request.Content = new StringContent(jsonRequest, System.Text.Encoding.UTF8, "application/json");
            request.Headers.Add("Authorization", $"EndpointKey {qnaEndpoint.EndpointKey}");
            return request;
        }

        private static void ValidateOptions(QnAMakerOptions options)
        {
            if (options.ScoreThreshold == 0)
            {
                options.ScoreThreshold = 0.3F;
            }

            if (options.Top == 0)
            {
                options.Top = 1;
            }

            if (options.ScoreThreshold < 0 || options.ScoreThreshold > 1)
            {
                throw new ArgumentOutOfRangeException(nameof(options.ScoreThreshold), "Score threshold should be a value between 0 and 1");
            }

            if (options.Timeout == 0.0D)
            {
                options.Timeout = 100000;
            }

            if (options.Top < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(options.Top), "Top should be an integer greater than 0");
            }

            if (options.StrictFilters == null)
            {
                options.StrictFilters = new Metadata[] { };
            }

            if (options.MetadataBoost == null)
            {
                options.MetadataBoost = new Metadata[] { };
            }
        }
    }
}
