using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Bot.Builder.Azure;
using Microsoft.Bot.Builder.Skills;
using Microsoft.Bot.Configuration;
using System.Collections.Generic;

namespace VirtualAssistantTemplate.Services
{
    public class BotSettings
    {
        public string MicrosoftAppId { get; set; }

        public string MicrosoftAppPassword { get; set; }

        public string DefaultLocale { get; set; }

        public CosmosDbStorageOptions CosmosDb { get; set; }

        public TelemetryConfiguration AppInsights { get; set; }

        public BlobStorageSettings BlobStorage { get; set; }

        public ContentModeratorSettings ContentModerator { get; set; }

        public Dictionary<string, CognitiveModelSettings> CognitiveModels { get; set; }

        public List<SkillDefinition> Skills { get; set; }

        public class BlobStorageSettings
        {
            public string ConnectionString { get; set; }
            public string Container { get; set; }
        }

        public class ContentModeratorSettings
        {
            public string Key { get; set; }
        }

        public class CognitiveModelSettings
        {
            public DispatchService DispatchModel { get; set; }

            public List<LuisService> LanguageModels { get; set; }

            public List<QnAMakerService> Knowledgebases { get; set; }
        }
    }
}
