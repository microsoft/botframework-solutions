namespace Microsoft.Bot.Builder.Solutions.Shared
{
    using System.Collections.Generic;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.Bot.Builder.Azure;
    using Microsoft.Bot.Builder.Solutions.Shared.Authentication;
    using Microsoft.Bot.Configuration;

    /// <summary>
    /// Base class representing the configuration for a bot.
    /// </summary>
    public class BotSettingsBase
    {
        /// <summary>
        /// Gets or sets the Microsoft Application Id.
        /// </summary>
        public string MicrosoftAppId { get; set; }

        /// <summary>
        /// Gets or sets the Microsoft Application Password.
        /// </summary>
        public string MicrosoftAppPassword { get; set; }

        /// <summary>
        /// Gets or sets the default locale of the bot.
        /// </summary>
        public string DefaultLocale { get; set; }

        /// <summary>
        /// Gets or sets the OAuth Connections for the bot.
        /// </summary>
        public List<OAuthConnection> OAuthConnections { get; set; }

        /// <summary>
        /// Gets or sets the CosmosDB Configuration for the bot.
        /// </summary>
        public CosmosDbStorageOptions CosmosDb { get; set; }

        /// <summary>
        /// Gets or sets the Application Insights configuration for the bot.
        /// </summary>
        public TelemetryConfiguration AppInsights { get; set; }

        /// <summary>
        /// Gets or sets the Azure Blob Storage configuration for the bot.
        /// </summary>
        public BlobStorageConfiguration BlobStorage { get; set; }

        /// <summary>
        /// Gets or sets the Azure Content Moderator configuration for the bot.
        /// </summary>
        public ContentModeratorConfiguration ContentModerator { get; set; }

        /// <summary>
        /// Gets or sets the dictionary of cognitive model configurations by locale for the bot.
        /// </summary>
        public Dictionary<string, CognitiveModelConfiguration> CognitiveModels { get; set; } = new Dictionary<string, CognitiveModelConfiguration>();

        /// <summary>
        /// Gets or sets the Properties dictionary.
        /// </summary>
        public Dictionary<string, string> Properties { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Class representing configuration for an Azure Blob Storage service.
        /// </summary>
        public class BlobStorageConfiguration
        {
            /// <summary>
            /// Gets or sets the connection string for the Azure Blob Storage service.
            /// </summary>
            public string ConnectionString { get; set; }

            /// <summary>
            /// Gets or sets the blob container for the Azure Blob Storage service.
            /// </summary>
            public string Container { get; set; }
        }

        /// <summary>
        /// Class representing configuration for an Azure Content Moderator service.
        /// </summary>
        public class ContentModeratorConfiguration
        {
            /// <summary>
            /// Gets or sets the subscription key for the Content Moderator service.
            /// </summary>
            public string Key { get; set; }
        }

        /// <summary>
        /// Class representing configuration for a collection of Azure Cognitive Models.
        /// </summary>
        public class CognitiveModelConfiguration
        {
            /// <summary>
            /// Gets or sets the Dispatch service for the set of cognitive models.
            /// </summary>
            public DispatchService DispatchModel { get; set; }

            /// <summary>
            /// Gets or sets the collection of LUIS models.
            /// </summary>
            public List<LuisService> LanguageModels { get; set; }

            /// <summary>
            /// Gets or sets the collection of QnA Maker knowledgebases.
            /// </summary>
            public List<QnAMakerService> Knowledgebases { get; set; }
        }
    }
}
