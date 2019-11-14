// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

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

        private Dictionary<string, IRecognizerConvert> TestUtterances { get; set; }

        public override Task<T> RecognizeAsync<T>(ITurnContext turnContext, CancellationToken cancellationToken)
        {
            var text = turnContext.Activity.Text;

            var mockGeneral = generalUtterancesManager.GetValueOrDefault(text, generalUtterancesManager.GetBaseNoneIntent());

            var test = mockGeneral as object;
            var mockResult = (T)test;
            return Task.FromResult(mockResult);
        }

        public void RegisterUtterances(Dictionary<string, IRecognizerConvert> utterances)
        {
            foreach (var utterance in utterances)
            {
                TestUtterances.Add(utterance.Key, utterance.Value);
            }
        }
    }
}