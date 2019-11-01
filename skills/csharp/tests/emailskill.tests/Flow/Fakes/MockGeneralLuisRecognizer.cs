using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EmailSkill.Tests.Flow.Utterances;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.AI.Luis;

namespace EmailSkill.Tests.Flow.Fakes
{
    public class MockGeneralLuisRecognizer : LuisRecognizer
    {
        private static LuisApplication mockApplication = new LuisApplication()
        {
            ApplicationId = "testappid",
            Endpoint = "testendpoint",
            EndpointKey = "testendpointkey"
        };

        private GeneralTestUtterances generalUtterancesManager;

        public MockGeneralLuisRecognizer()
            : base(application: mockApplication)
        {
            this.generalUtterancesManager = new GeneralTestUtterances();
        }

        public Task<RecognizerResult> RecognizeAsync(ITurnContext turnContext, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        private Dictionary<string, IRecognizerConvert> TestUtterances { get; set; }

        public void RegisterUtterances(Dictionary<string, IRecognizerConvert> utterances)
        {
            foreach (var utterance in utterances)
            {
                TestUtterances.Add(utterance.Key, utterance.Value);
            }
        }

        public override Task<T> RecognizeAsync<T>(ITurnContext turnContext, CancellationToken cancellationToken)
        {
            var text = turnContext.Activity.Text;

            var mockGeneral = generalUtterancesManager.GetValueOrDefault(text, generalUtterancesManager.GetBaseNoneIntent());

            var test = mockGeneral as object;
            var mockResult = (T)test;
            return Task.FromResult((T)mockResult);
        }
    }
}