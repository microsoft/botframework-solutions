using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;

namespace Microsoft.Bot.Solutions.Middleware.Telemetry
{
    public interface ITelemetryLuisRecognizer : IRecognizer
    {
        bool LogPersonalInformation { get; }

        Task<T> RecognizeAsync<T>(DialogContext dialogContext, bool logPersonalInformation, CancellationToken cancellationToken = default(CancellationToken))
            where T : IRecognizerConvert, new();

        Task<T> RecognizeAsync<T>(ITurnContext turnContext, bool logPersonalInformation, CancellationToken cancellationToken = default(CancellationToken))
            where T : IRecognizerConvert, new();
    }
}