using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EmailSkillTest.Flow.Utterances;
using Luis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Solutions.Middleware.Telemetry;

namespace EmailSkillTest.Flow.Fakes
{
    public class MockEmailLuisRecognizer : ITelemetryLuisRecognizer
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

        public bool LogOriginalMessage => throw new NotImplementedException();

        public bool LogUsername => throw new NotImplementedException();

        public void AddUtteranceManager(BaseTestUtterances utterancesManager)
        {
            this.emailUtterancesManager.AddManager(utterancesManager);
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

        public Task<T> RecognizeAsync<T>(DialogContext dialogContext, bool logOriginalMessage, CancellationToken cancellationToken = default(CancellationToken))
            where T : IRecognizerConvert, new()
        {
            throw new NotImplementedException();
        }

        public Task<T> RecognizeAsync<T>(ITurnContext turnContext, bool logOriginalMessage, CancellationToken cancellationToken = default(CancellationToken))
            where T : IRecognizerConvert, new()
        {
            throw new NotImplementedException();
        }
    }
}