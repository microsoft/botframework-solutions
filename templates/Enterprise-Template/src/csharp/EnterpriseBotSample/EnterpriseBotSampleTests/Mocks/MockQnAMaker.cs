using EnterpriseBotSample.Middleware.Telemetry;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.AI.Luis;
using Microsoft.Bot.Builder.AI.QnA;
using Microsoft.Bot.Builder.Dialogs;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EnterpriseBotSampleTests.Mocks
{
    public class MockQnAMaker : ITelemetryQnAMaker
    {
        public MockQnAMaker(QueryResult[] defaultAnswer)
        {
            TestAnswers = new Dictionary<string, QueryResult[]>();
            DefaultAnswer = defaultAnswer;
        }

        private Dictionary<string, QueryResult[]> TestAnswers { get; set; }

        private QueryResult[] DefaultAnswer { get; set; }

        public bool LogPersonalInformation => throw new NotImplementedException();

        public void RegisterAnswers(Dictionary<string, QueryResult[]> utterances)
        {
            foreach (var utterance in utterances)
            {
                TestAnswers.Add(utterance.Key, utterance.Value);
            }
        }

        public Task<QueryResult[]> GetAnswersAsync(ITurnContext context, QnAMakerOptions options = null)
        {
            var text = context.Activity.Text;

            var mockResult = TestAnswers.GetValueOrDefault(text, DefaultAnswer);
            return Task.FromResult(mockResult);
        }
    }
}
