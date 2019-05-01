using Luis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.AI.Luis;
using Microsoft.Bot.Builder.Dialogs;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PointOfInterestSkillTests.Flow.Fakes
{
    public class MockLuisRecognizer : ITelemetryRecognizer
    {
        public MockLuisRecognizer()
        {
        }

        public bool LogPersonalInformation { get; set; } = false;

        public IBotTelemetryClient TelemetryClient { get; set; } = new NullBotTelemetryClient();

        public Task<RecognizerResult> RecognizeAsync(ITurnContext turnContext, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<T> RecognizeAsync<T>(ITurnContext turnContext, CancellationToken cancellationToken)
            where T : IRecognizerConvert, new()
        {
            var mockResult = new T();

            var t = typeof(T);
            var text = turnContext.Activity.Text;
            if (t.Name.Equals(typeof(PointOfInterestLuis).Name))
            {
                var mockPointOfInterest = new MockPointOfInterestIntent(text);

                var test = mockPointOfInterest as object;
                mockResult = (T)test;
            }
            else if (t.Name.Equals(typeof(General).Name))
            {
                var mockGeneralIntent = new MockGeneralIntent(text);

                var test = mockGeneralIntent as object;
                mockResult = (T)test;
            }

            return Task.FromResult(mockResult);
        }

        public Task<T> RecognizeAsync<T>(DialogContext dialogContext, CancellationToken cancellationToken = default(CancellationToken))
            where T : IRecognizerConvert, new()
        {
            var mockResult = new T();

            var t = typeof(T);
            var text = dialogContext.Context.Activity.Text;
            if (t.Name.Equals(typeof(PointOfInterestLuis).Name))
            {
                var mockPointOfInterest = new MockPointOfInterestIntent(text);

                var test = mockPointOfInterest as object;
                mockResult = (T)test;
            }
            else if (t.Name.Equals(typeof(General).Name))
            {
                var mockGeneralIntent = new MockGeneralIntent(text);

                var test = mockGeneralIntent as object;
                mockResult = (T)test;
            }

            return Task.FromResult(mockResult);
        }

        public Task<RecognizerResult> RecognizeAsync(ITurnContext turnContext, Dictionary<string, string> telemetryProperties, Dictionary<string, double> telemetryMetrics, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public Task<T> RecognizeAsync<T>(ITurnContext turnContext, Dictionary<string, string> telemetryProperties, Dictionary<string, double> telemetryMetrics, CancellationToken cancellationToken = default(CancellationToken)) where T : IRecognizerConvert, new()
        {
            throw new NotImplementedException();
        }
    }
}