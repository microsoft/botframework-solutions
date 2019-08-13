using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Threading;
using LuisModelTest.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using static LuisModelTest.Models.LuisResult;
using static LuisModelTest.Models.LuisFileModel;
using static LuisModelTest.Models.LuisFileModel.Utterance;
using System.Runtime.InteropServices.ComTypes;
using System.Security.Cryptography;

namespace LuisModelTest
{
    class Program
    {
        static void Usage()
        {
            Console.Error.WriteLine("LuisModelTest <LUIS.json> [-s subscriptionKey] [-i applicationId] [-t trainFile]");
            Console.Error.WriteLine("Test the luis model with give testcases.");
            Console.Error.WriteLine("-s subscriptionKey, an azure subscription.");
            Console.Error.WriteLine("-i applicationId, a luis app id where to test the model");
            Console.Error.WriteLine("-t trainFile, [Optional] file to train a new model for testing");
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
            string testFileName = null;
            string applicationId = null;
            string subscriptionKey = null;
            string trainFileName = null;
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
                            case "-i":
                                {
                                    ++i;
                                    applicationId = NextArg(ref i, args);
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

            if (testFileName == null || applicationId == null || subscriptionKey == null)
            {
                Usage();
            }
            else
            {
                if (trainFileName != null)
                {
                    bool updated = UpdateLuis(subscriptionKey, applicationId, trainFileName).Result;
                    if (updated)
                    {
                        RunTest(subscriptionKey, applicationId, testFileName).Wait();
                    }
                }
                else
                {
                    RunTest(subscriptionKey, applicationId, testFileName).Wait();
                }
            }
            Console.WriteLine("Finished. Press any key to exit");
            Console.ReadKey();
        }

        static void Test()
        {
            var luisManager = new LuisManager.LuisManager("Config/luis_setting.json");
            var luisService = luisManager.LuisService;

            if (UpdateLuis(
                luisService.subscriptionKey,
                luisService.applicationId,
                luisService.luisFileName
                ).Result)
            {
                Console.WriteLine("Update finished. Start Testing");
                RunTest(luisService.subscriptionKey, luisService.applicationId, luisService.testFileName).Wait();
            }

            Console.WriteLine("Finished. Press any key to exit");
            Console.ReadKey();

        }

        private static async Task<bool> UpdateLuis(
            string subscriptionKey,
            string applicationId,
            string fileName)
        {
            // input file
            Console.WriteLine("Reading luis...");

            StreamReader srReadFile = new StreamReader(fileName);
            string jsonText = "";
            while (!srReadFile.EndOfStream)
            {
                jsonText += srReadFile.ReadLine();
            }

            srReadFile.Close();
            LuisFileModel luisFile = JsonConvert.DeserializeObject<LuisFileModel>(jsonText);
            string version = "0.1";
            if (luisFile.VersionId != null)
                version = luisFile.VersionId;
            Console.WriteLine("Done");

            // try delete same id
            Console.WriteLine("Delete version...");
            HttpResponseMessage response = DeleteExistVersion(subscriptionKey, version, applicationId).Result;
            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                Console.WriteLine("Done");
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
            {
                Console.WriteLine("Rename version...");
                HttpResponseMessage resp = RenameExistVersion(subscriptionKey, version, applicationId).Result;
                if(resp.StatusCode != System.Net.HttpStatusCode.OK)
                {
                    Console.WriteLine("Renaming failed");
                    Console.WriteLine(response.StatusCode);
                    return false;
                }
                Console.WriteLine("Done");
            }
            else
            {
                Console.WriteLine("Deleting failed");
                Console.WriteLine(response.StatusCode);
            }

            // upload luis
            Console.WriteLine("Uploading...");
            response = UploadNewJsonFile(jsonText, subscriptionKey, version, applicationId).Result;
            if (response.StatusCode != System.Net.HttpStatusCode.Created)
            {
                Console.WriteLine("Uploading failed");
                Console.WriteLine(response.StatusCode);
                return false;
            }

            Console.WriteLine("Done");

            // train model
            Console.WriteLine("Training...");
            response = Train(subscriptionKey, applicationId, version).Result;
            if (response.StatusCode != System.Net.HttpStatusCode.Accepted)
            {
                Console.WriteLine("Training failed");
                Console.WriteLine(response.StatusCode);
                return false;
            }
         
            //get training status
            bool flag;
            do
            {
                Console.WriteLine("Check training status...");
                flag = true;
                response = GetTrainingStatus(subscriptionKey, applicationId, version).Result;
                JArray jarray = (JArray)JsonConvert.DeserializeObject(response.Content.ReadAsStringAsync().Result);
                foreach (var item in jarray)
                {
                    if (item["details"]["status"].ToString() != "Success")
                    {
                        flag = false;
                        break;
                    }
                }
                Thread.Sleep(3000);
            } while (flag == false);

            Console.WriteLine("Done");

            // publish model
            Console.WriteLine("Publish...");

            response = Publish(subscriptionKey, applicationId, version).Result;
            Console.WriteLine(response.StatusCode);
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine("Publish failed");
                Console.WriteLine(response.StatusCode);
                return false;
            }

            Console.WriteLine("Done");
            return true;
        }

        private static async Task RunTest(
            string subscriptionKey,
            string applicationId,
            string fileName)
        {
            // input file
            Console.WriteLine("Reading test set...");

            StreamReader srReadFile = new StreamReader(fileName);
            string jsonText = "";
            while (!srReadFile.EndOfStream)
            {
                jsonText += srReadFile.ReadLine();
            }

            srReadFile.Close();
            TestSetModel testSet = JsonConvert.DeserializeObject<TestSetModel>(jsonText);
            Console.WriteLine("Done");


            Console.WriteLine("Start testing...");
            StreamWriter output = new StreamWriter(fileName + "_error_case.json");
            int passed = 0;
            int total = 0;
            foreach (Utterance utterance in testSet.Utterances)
            {
                total++;
                bool isPass = true;
                string query = utterance.Text;
                LuisResult result = GetLuisResult(subscriptionKey, applicationId, query).Result;
                if (result.TopScoringIntent.Intent != utterance.Intent)
                {
                    isPass = false;
                }

                foreach (EntityResult entityResult in result.Entities)
                {
                    if (!isPass)
                    {
                        break;
                    }
                    isPass &= IsCorrectEntity(entityResult, utterance.Entities);
                }
                if (isPass)
                {
                    passed++;
                }
                else
                {
                    output.WriteLine("Luis Result:");
                    output.WriteLine(JsonConvert.SerializeObject(result, Formatting.Indented));
                    output.WriteLine("Expected:");
                    output.WriteLine(JsonConvert.SerializeObject(utterance, Formatting.Indented));
                    output.WriteLine("");
                    Console.WriteLine(query);
                }
                //break;
            }
            output.Flush();
            output.Close();
            Console.WriteLine("Total: " + total);
            Console.WriteLine("Passed: " + passed);
        }


        private static bool IsCorrectEntity(EntityResult entityResult, List<LabelEntity> labelEntities)
        {
            if (entityResult.Type.StartsWith("builtin"))
            {
                return true;
            }

            foreach (LabelEntity labelEntity in labelEntities)
            {
                if (entityResult.Type != labelEntity.Entity)
                {
                    continue;
                }
                else 
                if (entityResult.StartIndex != labelEntity.StartPos)
                {
                    continue;
                }
                else
                if (entityResult.EndIndex != labelEntity.EndPos)
                {
                    continue;
                }
                else
                {
                    return true;
                }

            }
            return false;
        }

        private static async Task<HttpResponseMessage> UploadNewJsonFile(string body, string subscriptionKey, string versionId, string appId)
        {
            var client = new HttpClient();
            var queryString = HttpUtility.ParseQueryString(string.Empty);

            // Request headers
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", subscriptionKey);

            // Request parameters
            queryString["versionId"] = versionId;
            var uri = "https://westus.api.cognitive.microsoft.com/luis/api/v2.0/apps/" + appId + "/versions/import?" + queryString;

            HttpResponseMessage response;

            // Request body
            byte[] byteData = Encoding.UTF8.GetBytes(body);

            using (var content = new ByteArrayContent(byteData))
            {
                content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                response = await client.PostAsync(uri, content);
            }

            return response;
        }

        private static async Task<HttpResponseMessage> DeleteExistVersion(string subscriptionKey, string versionId, string appId)
        {
            var client = new HttpClient();
            var queryString = HttpUtility.ParseQueryString(string.Empty);

            // Request headers
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", subscriptionKey);

            var uri = "https://westus.api.cognitive.microsoft.com/luis/api/v2.0/apps/" + appId + "/versions/" + versionId + "/?" + queryString;

            HttpResponseMessage response = await client.DeleteAsync(uri);

            return response;
        }


        private static async Task<HttpResponseMessage> RenameExistVersion(string subscriptionKey, string versionId, string appId)
        {
            var client = new HttpClient();
            var queryString = HttpUtility.ParseQueryString(string.Empty);

            // Request headers
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", subscriptionKey);

            var uri = "https://westus.api.cognitive.microsoft.com/luis/api/v2.0/apps/" + appId + "/versions/" + versionId + "/?" + queryString;

            HttpResponseMessage response;

            // Request body
            byte[] byteData = Encoding.UTF8.GetBytes("{ \"version\": \"" + versionId + ".bk" + "\"}");

            using (var content = new ByteArrayContent(byteData))
            {
                content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                response = await client.PutAsync(uri, content);
            }

            return response;
        }

        private static async Task<HttpResponseMessage> Train(string subscriptionKey, string appId, string versionId)
        {
            var client = new HttpClient();
            var queryString = HttpUtility.ParseQueryString(string.Empty);

            // Request headers
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", subscriptionKey);

            var uri = "https://westus.api.cognitive.microsoft.com/luis/api/v2.0/apps/" + appId + "/versions/" + versionId + "/train?" + queryString;

            HttpResponseMessage response = null;

            // Request body
            byte[] byteData = Encoding.UTF8.GetBytes("{}");

            using (var content = new ByteArrayContent(byteData))
            {
                content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                response = await client.PostAsync(uri, content);
            }

            return response;
        }

        private static async Task<HttpResponseMessage> GetTrainingStatus(string subscriptionKey, string appId, string versionId)
        {
            var client = new HttpClient();
            var queryString = HttpUtility.ParseQueryString(string.Empty);

            // Request headers
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", subscriptionKey);

            var uri = "https://westus.api.cognitive.microsoft.com/luis/api/v2.0/apps/" + appId + "/versions/" + versionId + "/train?" + queryString;

            HttpResponseMessage response = await client.GetAsync(uri);
            return response;
        }
    

        private static async Task<HttpResponseMessage> Publish(string subscriptionKey, string appId, string versionId)
        {
            var client = new HttpClient();
            var queryString = HttpUtility.ParseQueryString(string.Empty);

            // Request headers
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", subscriptionKey);

            var uri = "https://westus.api.cognitive.microsoft.com/luis/api/v2.0/apps/" + appId + "/publish?" + queryString;

            HttpResponseMessage response;

            // Request body
            PublishRequest publishRequest = new PublishRequest() { VersionId = versionId, IsStaging = false, DirectVersionPublish = false };
            byte[] byteData = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(publishRequest, Formatting.Indented));

            using (var content = new ByteArrayContent(byteData))
            {
                content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                response = await client.PostAsync(uri, content);
            }

            return response;
        }

        private static async Task<LuisResult> GetLuisResult(string subscriptionKey, string appId, string query)
        {
            var client = new HttpClient();
            var queryString = HttpUtility.ParseQueryString(string.Empty);
            queryString["verbose"] = "true";
            queryString["timezoneOffset"] = "-360";
            queryString["subscription-key"] = subscriptionKey;
            queryString["q"] = query;

            // Request headers
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", subscriptionKey);

            var uri = "https://westus.api.cognitive.microsoft.com/luis/v2.0/apps/" + appId + "?" + queryString;

            HttpResponseMessage response = await client.GetAsync(uri);
            string result = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<LuisResult>(result);
        }

        private class PublishRequest
        {

            [JsonProperty("versionId")]
            public string VersionId { get; set; }

            [JsonProperty("isStaging")]
            public bool IsStaging { get; set; }

            [JsonProperty("directVersionPublish")]
            public bool DirectVersionPublish { get; set; }
        }

        private class StatusList
        {
            [JsonProperty("TrainingStatusList")]
            public List<TrainingStatus> TrainingStatusList { get; set; }

            public class TrainingStatus
            {
                [JsonProperty("modelId")]
                public string ModelId { get; set; }

                [JsonProperty("details")]
                public DetailsData Details { get; set; }

                public class DetailsData
                {
                    [JsonProperty("statusId")]
                    public int StatusId { get; set; }

                    [JsonProperty("status")]
                    public string Status { get; set; }

                    [JsonProperty("exampleCount")]
                    public int ExampleCount { get; set; }

                    [JsonProperty("trainingDateTime")]
                    public string TrainingDateTime { get; set; }
                }
            }
        }
    }
}
