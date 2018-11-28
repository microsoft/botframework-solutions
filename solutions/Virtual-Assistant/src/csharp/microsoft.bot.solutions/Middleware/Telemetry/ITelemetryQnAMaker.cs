using System;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.AI.QnA;

namespace Microsoft.Bot.Solutions
{
    public interface ITelemetryQnAMaker
    {
        bool LogOriginalMessage { get; }
        bool LogUserName { get; }

        Task<QueryResult[]> GetAnswersAsync(ITurnContext context);
    }
}