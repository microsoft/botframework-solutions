using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EmailSkillTest.Flow.Utterances;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Solutions.Shared.Telemetry;

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

        public MockEmailLuisRecognizer(params BaseTestUtterances[] utterancesManagers)
        {
            this.emailUtterancesManager = new BaseTestUtterances();

            foreach (var manager in utterancesManagers)
            {
                foreach (var pair in manager)
                {
                    this.emailUtterancesManager.TryAdd(pair.Key, pair.Value);
                }
            }
        }

        public bool LogPersonalInformation => throw new NotImplementedException();

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
            var mockEmail = emailUtterancesManager.GetValueOrDefault(text, emailUtterancesManager.GetBaseNoneIntent());

            var test = mockEmail as object;
            var mockResult = (T)test;

            return Task.FromResult(mockResult);
        }

        public Task<T> RecognizeAsync<T>(DialogContext dialogContext, CancellationToken cancellationToken = default(CancellationToken))
            where T : IRecognizerConvert, new()
        {
            throw new NotImplementedException();
        }
    }
}