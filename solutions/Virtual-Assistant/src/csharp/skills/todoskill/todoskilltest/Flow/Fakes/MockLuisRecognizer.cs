using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Luis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Solutions.Middleware.Telemetry;
using ToDoSkillTest.Flow.Utterances;

namespace ToDoSkillTest.Flow.Fakes
{
    public class MockLuisRecognizer : ITelemetryLuisRecognizer
    {
        private BaseTestUtterances utterancesManager;
        private GeneralTestUtterances generalUtterancesManager;

        public MockLuisRecognizer(BaseTestUtterances utterancesManager)
        {
            this.utterancesManager = utterancesManager;
        }

        public MockLuisRecognizer(GeneralTestUtterances generalUtterancesMananger)
        {
            this.generalUtterancesManager = generalUtterancesMananger;
        }

        public bool LogOriginalMessage => throw new NotImplementedException();

        public bool LogUsername => throw new NotImplementedException();

        public Task<RecognizerResult> RecognizeAsync(ITurnContext turnContext, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<T> RecognizeAsync<T>(ITurnContext turnContext, CancellationToken cancellationToken)
            where T : IRecognizerConvert, new()
        {
            T mockResult = new T();

            Type t = typeof(T);
            var text = turnContext.Activity.Text;
            if (t.Name.Equals(typeof(ToDo).Name))
            {
                ToDo mockToDo = utterancesManager.GetValueOrDefault(text, utterancesManager.GetBaseNoneIntent());

                var test = mockToDo as object;
                mockResult = (T)test;
            }
            else if (t.Name.Equals(typeof(General).Name))
            {
                General mockGeneralIntent = generalUtterancesManager.GetValueOrDefault(text, generalUtterancesManager.GetBaseNoneIntent());

                var test = mockGeneralIntent as object;
                mockResult = (T)test;
            }

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