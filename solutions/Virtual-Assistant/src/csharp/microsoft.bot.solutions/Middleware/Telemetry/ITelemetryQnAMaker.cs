using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.AI.QnA;

namespace Microsoft.Bot.Solutions.Middleware.Telemetry
{
    public interface ITelemetryQnAMaker
    {
        bool LogOriginalMessage { get; }

        bool LogUserName { get; }

        Task<QueryResult[]> GetAnswersAsync(ITurnContext context);
    }
}