using System.Threading.Tasks;
using Microsoft.Bot.Builder.AI.QnA;

namespace Microsoft.Bot.Builder.Solutions.Telemetry
{
    public interface ITelemetryQnAMaker
    {
        bool LogPersonalInformation { get; }

        Task<QueryResult[]> GetAnswersAsync(ITurnContext context, QnAMakerOptions options = null);
    }
}