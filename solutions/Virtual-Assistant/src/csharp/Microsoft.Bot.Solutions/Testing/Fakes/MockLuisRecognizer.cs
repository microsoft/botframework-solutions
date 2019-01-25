using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Solutions.Middleware.Telemetry;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Bot.Solutions.Testing.Fakes
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

        public void RegisterUtterances(Dictionary<string, IRecognizerConvert> utterances)
        {
            foreach (var utterance in utterances)
            {
                TestUtterances.Add(utterance.Key, utterance.Value);
            }
        }

        public bool LogOriginalMessage => throw new NotImplementedException();

        public bool LogUsername => throw new NotImplementedException();

        public Task<RecognizerResult> RecognizeAsync(ITurnContext turnContext, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<T> RecognizeAsync<T>(DialogContext dialogContext, bool logOriginalMessage, CancellationToken cancellationToken = default(CancellationToken))
            where T : IRecognizerConvert, new()
        {
            var text = dialogContext.Context.Activity.Text;

            var mockResult = TestUtterances.GetValueOrDefault(text, DefaultIntent);
            return Task.FromResult((T)mockResult);
        }

        public Task<T> RecognizeAsync<T>(ITurnContext turnContext, bool logOriginalMessage, CancellationToken cancellationToken = default(CancellationToken))
            where T : IRecognizerConvert, new()
        {
            var text = turnContext.Activity.Text;

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
