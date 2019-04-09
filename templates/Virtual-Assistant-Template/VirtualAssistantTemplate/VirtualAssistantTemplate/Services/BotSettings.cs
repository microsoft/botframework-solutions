using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Bot.Builder.Azure;
using Microsoft.Bot.Builder.Skills;
using Microsoft.Bot.Builder.Solutions.Shared;
using Microsoft.Bot.Builder.Solutions.Shared.Authentication;
using System.Collections.Generic;

namespace VirtualAssistantTemplate.Services
{
    public class BotSettings : BotSettingsBase
    {
        public List<OAuthConnection> OAuthConnections { get; set; }

        public CosmosDbStorageOptions CosmosDb { get; set; }

        public TelemetryConfiguration AppInsights { get; set; }

        public BlobStorageConfiguration BlobStorage { get; set; }

        public ContentModeratorConfiguration ContentModerator { get; set; }
        public string DefaultLocale { get; set; }

        public Dictionary<string, CognitiveModelConfiguration> CognitiveModels { get; set; }

        public List<SkillDefinition> Skills { get; set; }
    }
}