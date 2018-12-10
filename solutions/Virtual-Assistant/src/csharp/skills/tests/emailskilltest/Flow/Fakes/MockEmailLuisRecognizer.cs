using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EmailSkillTest.Flow.Utterances;
using Luis;
using Microsoft.Bot.Builder;

namespace EmailSkillTest.Flow.Fakes
{
    public class MockEmailLuisRecognizer : IRecognizer
    {
        private BaseTestUtterances emailUtterancesManager;

        public MockEmailLuisRecognizer()
        {
            this.emailUtterancesManager = new BaseTestUtterances();
        }

        public MockEmailLuisRecognizer(BaseTestUtterances utterancesManager)
        {
            this.emailUtterancesManager = utterancesManager;
        }

        public Task<RecognizerResult> RecognizeAsync(ITurnContext turnContext, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<T> RecognizeAsync<T>(ITurnContext turnContext, CancellationToken cancellationToken)
            where T : IRecognizerConvert, new()
        {
            var text = turnContext.Activity.Text;
            Email mockEmail = emailUtterancesManager.GetValueOrDefault(text, emailUtterancesManager.GetBaseNoneIntent());

            var test = mockEmail as object;
            var mockResult = (T)test;

            return Task.FromResult(mockResult);
        }
    }
}