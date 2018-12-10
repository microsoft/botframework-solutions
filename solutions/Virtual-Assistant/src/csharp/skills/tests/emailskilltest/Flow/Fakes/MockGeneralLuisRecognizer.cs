using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EmailSkillTest.Flow.Utterances;
using Luis;
using Microsoft.Bot.Builder;

namespace EmailSkillTest.Flow.Fakes
{
    public class MockGeneralLuisRecognizer : IRecognizer
    {
        private GeneralTestUtterances generalUtterancesManager;

        public MockGeneralLuisRecognizer()
        {
            this.generalUtterancesManager = new GeneralTestUtterances();
        }

        public Task<RecognizerResult> RecognizeAsync(ITurnContext turnContext, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<T> RecognizeAsync<T>(ITurnContext turnContext, CancellationToken cancellationToken)
            where T : IRecognizerConvert, new()
        {
            var text = turnContext.Activity.Text;

            General mockGeneral = generalUtterancesManager.GetValueOrDefault(text, generalUtterancesManager.GetBaseNoneIntent());

            var test = mockGeneral as object;
            var mockResult = (T)test;

            return Task.FromResult(mockResult);
        }
    }
}