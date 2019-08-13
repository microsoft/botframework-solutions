using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using QnAMakerTest.Models;
using Microsoft.Bot.Builder.AI.QnA;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace QnAMakerTest
{
    class Program
    {
        static HttpClient client;
        static void Usage()
        {
            Console.Error.WriteLine("QnAMakerTest <QnA.json> [-h hostname] [-e endpointKey] [-k kbId] [-t trainFile] [-s subscriptionKey] [-v] [--version]");
            Console.Error.WriteLine("Test a qna model with giving testcases");
            Console.Error.WriteLine("-h hostname, hosturl like https://*.azurewebsites.net/qnamaker");
            Console.Error.WriteLine("-e endpointKey, EndpointKey for Authorization.");
            Console.Error.WriteLine("-k kbId, knowledgebaseId");
            Console.Error.WriteLine("-t trainFile, [Optional] file to train a new model for testing");
            Console.Error.WriteLine("-s subscriptionKey, [Optional] required if trainFile is inputed.");
            System.Environment.Exit(-1);
        }
        static string NextArg(ref int i, string[] args, bool optional = false, bool allowCmd = false)
        {
            string arg = null;
            if (i < args.Length)
            {
                arg = args[i];
                if (arg.StartsWith("{"))
                {
                    while (!args[i].EndsWith("}") && ++i < args.Length) ;
                    ++i;
                }
                arg = null;
                if (allowCmd)
                {
                    if (i < args.Length)
                    {
                        arg = args[i];
                    }
                }
                else
                {
                    if (i < args.Length && !args[i].StartsWith('-'))
                    {
                        arg = args[i];
                    }
                    else if (!optional)
                    {
                        Usage();
                    }
                    else
                    {
                        --i;
                    }
                }
            }
            return arg?.Trim();
        }
        static void Main(string[] args)
        {
            client = new HttpClient();
            string kbId = null;
            string subscriptionKey = null;
            string trainFileName = null;
            string endpointKey = null;
            string hostname = null;
            string testFileName = null;
            for (var i = 0; i < args.Length; ++i)
            {
                var arg = NextArg(ref i, args, allowCmd: true);
                if (arg != null)
                {
                    if (arg.StartsWith('-'))
                    {
                        arg = arg.ToLower();
                        switch (arg)
                        {
                            case "-s":
                                {
                                    ++i;
                                    subscriptionKey = NextArg(ref i, args);
                                }
                                break;
                            case "-h":
                                {
                                    ++i;
                                    hostname = NextArg(ref i, args);
                                    break;
                                }
                            case "-k":
                                {
                                    ++i;
                                    kbId = NextArg(ref i, args);
                                }
                                break;
                            case "-e":
                                {
                                    ++i;
                                    endpointKey = NextArg(ref i, args);
                                }
                                break;
                            case "-t":
                                {
                                    ++i;
                                    trainFileName = NextArg(ref i, args);
                                }
                                break;
                            case "-v":
                            case "--version":
                                Console.WriteLine($"0.1");
                                break;
                            default:
                                Usage();
                                break;
                        }
                    }
                    else if (testFileName == null)
                    {
                        testFileName = arg;
                    }
                    else
                    {
                        Usage();
                    }
                }
            }

            if(testFileName == null || kbId == null || hostname == null || endpointKey == null)
            {
                Usage();
            }
            else if(trainFileName != null && subscriptionKey == null)
            {
                Usage();
            }
            else
            {
                if(trainFileName!=null)
                {
                    bool replaced = ReplaceQnAMakerKB(kbId, subscriptionKey, trainFileName).Result;
                    if (replaced)
                    {
                        bool published = PublishQnAMakerKB(kbId, subscriptionKey).Result;
                        if (published)
                        {
                            RunTest(kbId, endpointKey, hostname, testFileName).Wait();
                        }
                    }
                }
                else
                {
                    RunTest(kbId, endpointKey, hostname, testFileName).Wait();
                }
                Console.WriteLine("Finished. Press any key to exit");
                Console.ReadKey();
            }
        }
        static void Test()
        { 
            var qnamakerManager = new QnAmakerManager.QnAMakerManager("knowledgebases_settings.json");
            var qnamakerService = qnamakerManager.QnAMakerService;
            //client = new HttpClient();

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
