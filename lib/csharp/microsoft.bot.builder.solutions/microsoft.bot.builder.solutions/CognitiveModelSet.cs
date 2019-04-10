using System.Collections.Generic;
using Microsoft.Bot.Builder.Solutions.Telemetry;

namespace Microsoft.Bot.Builder.Solutions
{
    public class CognitiveModelSet
    {
        public IRecognizer DispatchService { get; set; }

        public Dictionary<string, IRecognizer> LuisServices { get; set; } = new Dictionary<string, IRecognizer>();

        public Dictionary<string, ITelemetryQnAMaker> QnAServices { get; set; } = new Dictionary<string, ITelemetryQnAMaker>();
    }
}