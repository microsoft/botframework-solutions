using EnterpriseBotSampleTests.Mocks;
using EnterpriseBotSampleTests.Utterances;
using Microsoft.Bot.Builder.AI.QnA;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.IO;

namespace EnterpriseBotSampleTests.LUTestUtils
{
    public class ChitchatTestUtil
    {
        private static Dictionary<string, QueryResult[]> _utterances = new Dictionary<string, QueryResult[]>
        {
            { ChitchatUtterances.Greeting, CreateAnswer(@"Resources\chitchat_greeting.json") },
        };

        public static MockQnAMaker CreateRecognizer()
        {
            var recognizer = new MockQnAMaker(defaultAnswer: CreateAnswer(@"Resources\chitchat_default.json"));
            recognizer.RegisterAnswers(_utterances);
            return recognizer;
        }

        public static QueryResult[] CreateAnswer(string jsonPath)
        {
            var content = File.ReadAllText(jsonPath);
            dynamic result = JsonConvert.DeserializeObject(content);
            return result.answers.ToObject<QueryResult[]>();
        }
    }
}
