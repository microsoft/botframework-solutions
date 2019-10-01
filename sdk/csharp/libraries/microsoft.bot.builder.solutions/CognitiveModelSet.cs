using System.Collections.Generic;
using Microsoft.Bot.Builder.AI.Luis;
using Microsoft.Bot.Builder.AI.QnA;

namespace Microsoft.Bot.Builder.Solutions
{
    public class CognitiveModelSet
    {
        public IRecognizer DispatchService { get; set; }

        public Dictionary<string, ITelemetryRecognizer> LuisServices { get; set; } = new Dictionary<string, ITelemetryRecognizer>();

        public Dictionary<string, ITelemetryQnAMaker> QnAServices { get; set; } = new Dictionary<string, ITelemetryQnAMaker>();
    }
}