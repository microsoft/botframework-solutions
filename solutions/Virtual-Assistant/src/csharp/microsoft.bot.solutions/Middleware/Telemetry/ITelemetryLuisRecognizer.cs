using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;

namespace Microsoft.Bot.Solutions
{
    public interface ITelemetryLuisRecognizer : IRecognizer
    {
        bool LogOriginalMessage { get; }

        bool LogUsername { get; }

        Task<RecognizerResult> RecognizeAsync(ITurnContext context, CancellationToken cancellationToken, bool logOriginalMessage);

        Task<T> RecognizeAsync<T>(ITurnContext context, CancellationToken cancellationToken, bool logOriginalMessage)
            where T : IRecognizerConvert, new();
    }
}