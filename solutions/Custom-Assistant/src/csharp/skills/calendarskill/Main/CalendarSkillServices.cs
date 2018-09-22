using Microsoft.ApplicationInsights;
using Microsoft.Bot.Builder.AI.Luis;
using Microsoft.Bot.Builder.Azure;

namespace CalendarSkill
{
    public class CalendarSkillServices
    {
        public string AuthConnectionName { get; set; }

        public CosmosDbStorageOptions CosmosDbOptions { get; set; }

        public LuisRecognizer LuisRecognizer { get; set; }

        public TelemetryClient TelemetryClient { get; set; }
    }
}
