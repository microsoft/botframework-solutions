﻿using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Solutions.Telemetry;

namespace VirtualAssistantTemplate.Tests.Mocks
{
    public class MockLuisRecognizer : ITelemetryLuisRecognizer
    {
        public MockLuisRecognizer(IRecognizerConvert defaultIntent)
        {
            TestUtterances = new Dictionary<string, IRecognizerConvert>();
            DefaultIntent = defaultIntent;
        }

        private Dictionary<string, IRecognizerConvert> TestUtterances { get; set; }

        private IRecognizerConvert DefaultIntent { get; set; }

        public bool LogPersonalInformation => throw new NotImplementedException();

        public void RegisterUtterances(Dictionary<string, IRecognizerConvert> utterances)
        {
            foreach (var utterance in utterances)
            {
                TestUtterances.Add(utterance.Key, utterance.Value);
            }
        }

        public Task<RecognizerResult> RecognizeAsync(ITurnContext turnContext, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<T> RecognizeAsync<T>(DialogContext dialogContext, CancellationToken cancellationToken = default(CancellationToken))
            where T : IRecognizerConvert, new()
        {
            var text = dialogContext.Context.Activity.Text;

            var mockResult = TestUtterances.GetValueOrDefault(text, DefaultIntent);
            return Task.FromResult((T)mockResult);
        }

        public Task<T> RecognizeAsync<T>(ITurnContext turnContext, CancellationToken cancellationToken)
            where T : IRecognizerConvert, new()
        {
            var text = turnContext.Activity.Text;

            var mockResult = TestUtterances.GetValueOrDefault(text, DefaultIntent);
            return Task.FromResult((T)mockResult);
        }
    }
}