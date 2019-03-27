using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.AI.QnA;
using Microsoft.Bot.Builder.Solutions.Telemetry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace VirtualAssistantTemplate.Configuration
{
    public class CognitiveModelSet
    {
        public IRecognizer DispatchService { get; set; }

        public Dictionary<string, IRecognizer> LuisServices { get; set; } = new Dictionary<string, IRecognizer>();

        public Dictionary<string, QnAMaker> QnAServices { get; set; } = new Dictionary<string, QnAMaker>();
    }
}
