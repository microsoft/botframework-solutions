using System.Collections.Generic;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.AI.QnA;

namespace Microsoft.Bot.Solutions.Skills
{
    public class LocaleConfiguration
    {
        public string Locale { get; set; }

        public TelemetryLuisRecognizer DispatchRecognizer { get; set; }

        public Dictionary<string, IRecognizer> LuisServices { get; set; } = new Dictionary<string, IRecognizer>();

        public Dictionary<string, QnAMaker> QnAServices { get; set; } = new Dictionary<string, QnAMaker>();
    }
}
