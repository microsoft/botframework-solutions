// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.AI.Luis;
using Microsoft.Bot.Builder.Dialogs;
using PointOfInterestSkill.Tests.Flow.Utterances;

namespace PointOfInterestSkill.Tests.Flow.Fakes
{
    public class MockPointOfInterestLuisRecognizer : ITelemetryRecognizer
    {
        private BaseTestUtterances poiUtterancesManager;

        public MockPointOfInterestLuisRecognizer()
        {
            this.poiUtterancesManager = new BaseTestUtterances();
        }

        public MockPointOfInterestLuisRecognizer(BaseTestUtterances utterancesManager)
        {
            this.poiUtterancesManager = utterancesManager;
        }

        public MockPointOfInterestLuisRecognizer(params BaseTestUtterances[] utterancesManagers)
        {
            this.poiUtterancesManager = new BaseTestUtterances();

            foreach (var manager in utterancesManagers)
            {
                foreach (var pair in manager)
                {
                    this.poiUtterancesManager.TryAdd(pair.Key, pair.Value);
                }
            }
        }

        public bool LogPersonalInformation { get; set; } = false;

        public IBotTelemetryClient TelemetryClient { get; set; } = new NullBotTelemetryClient();

        public void AddUtteranceManager(BaseTestUtterances utterancesManager)
        {
            this.poiUtterancesManager.AddManager(utterancesManager);
        }

        public Task<RecognizerResult> RecognizeAsync(ITurnContext turnContext, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<T> RecognizeAsync<T>(ITurnContext turnContext, CancellationToken cancellationToken)
            where T : IRecognizerConvert, new()
        {
            var text = turnContext.Activity.Text;
            var mockEmail = poiUtterancesManager.GetValueOrDefault(text, poiUtterancesManager.GetBaseNoneIntent());

            var test = mockEmail as object;
            var mockResult = (T)test;

            return Task.FromResult(mockResult);
        }

        public Task<T> RecognizeAsync<T>(DialogContext dialogContext, CancellationToken cancellationToken = default(CancellationToken))
            where T : IRecognizerConvert, new()
        {
            throw new NotImplementedException();
        }

        public Task<RecognizerResult> RecognizeAsync(ITurnContext turnContext, Dictionary<string, string> telemetryProperties, Dictionary<string, double> telemetryMetrics, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public Task<T> RecognizeAsync<T>(ITurnContext turnContext, Dictionary<string, string> telemetryProperties, Dictionary<string, double> telemetryMetrics, CancellationToken cancellationToken = default(CancellationToken))
            where T : IRecognizerConvert, new()
        {
            throw new NotImplementedException();
        }
    }
}
