using Microsoft.Bot.Builder.Skills;
using Microsoft.Bot.Configuration;
using System.Collections.Generic;

namespace VirtualAssistantTemplate.Configuration
{
    public class BotSettings
    {
        public string MicrosoftAppId { get; set; }

        public string MicrosoftAppPassword { get; set; }

        public string DefaultLocale { get; set; }

        public CosmosDbSettings CosmosDb { get; set; }

        public AppInsightsSettings AppInsights { get; set; }

        public BlobStorageSettings BlobStorage { get; set; }

        public ContentModeratorSettings ContentModerator { get; set; }

        public Dictionary<string, CognitiveModelSettings> CognitiveModels { get; set; }

        public List<SkillDefinition> Skills { get; set; }

        public class AppInsightsSettings
        {
            public string AppId { get; set; }
            public string InstrumentationKey { get; set; }
        }

        public class BlobStorageSettings
        {
            public string ConnectionString { get; set; }
            public string Container { get; set; }
        }

        public class CosmosDbSettings
        {
            public string Endpoint { get; set; }
            public string Key { get; set; }
            public string Collection { get; set; }
            public string Database { get; set; }
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
