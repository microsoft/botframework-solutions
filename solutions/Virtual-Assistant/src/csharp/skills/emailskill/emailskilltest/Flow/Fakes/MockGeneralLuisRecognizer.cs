﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EmailSkillTest.Flow.Utterances;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Solutions.Telemetry;

namespace EmailSkillTest.Flow.Fakes
{
    public class MockGeneralLuisRecognizer : ITelemetryLuisRecognizer
    {
        private GeneralTestUtterances generalUtterancesManager;

        public MockGeneralLuisRecognizer()
        {
            this.generalUtterancesManager = new GeneralTestUtterances();
        }

        public bool LogPersonalInformation => throw new NotImplementedException();

        public Task<RecognizerResult> RecognizeAsync(ITurnContext turnContext, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<T> RecognizeAsync<T>(ITurnContext turnContext, CancellationToken cancellationToken)
            where T : IRecognizerConvert, new()
        {
            var text = turnContext.Activity.Text;

            var mockGeneral = generalUtterancesManager.GetValueOrDefault(text, generalUtterancesManager.GetBaseNoneIntent());

            var test = mockGeneral as object;
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