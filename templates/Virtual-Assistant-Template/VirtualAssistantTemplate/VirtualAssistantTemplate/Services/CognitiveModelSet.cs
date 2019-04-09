using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.AI.QnA;
using System.Collections.Generic;

namespace VirtualAssistantTemplate.Services
{
    public class CognitiveModelSet
    {
        public IRecognizer DispatchService { get; set; }

        public Dictionary<string, IRecognizer> LuisServices { get; set; } = new Dictionary<string, IRecognizer>();

        public Dictionary<string, QnAMaker> QnAServices { get; set; } = new Dictionary<string, QnAMaker>();
    }
}
