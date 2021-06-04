// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Bot.Solutions
{
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.Bot.Builder.Azure;
    using Microsoft.Bot.Solutions.Authentication;
    using Microsoft.Bot.Solutions.Services;

    /// <summary>
    /// Base class representing the configuration for a bot.
    /// </summary>
    [ExcludeFromCodeCoverageAttribute]
    public class BotSettingsBase
    {
        /// <summary>
        /// Gets or sets the Microsoft Application Id.
        /// </summary>
        /// <value>
        /// The Microsoft Application Id.
        /// </value>
        public string MicrosoftAppId { get; set; }

        /// <summary>
        /// Gets or sets the Microsoft Application Password.
        /// </summary>
        /// <value>
        /// The Microsoft Application Password.
        /// </value>
        public string MicrosoftAppPassword { get; set; }

        /// <summary>
        /// Gets or sets the default locale of the bot.
        /// </summary>
        /// <value>
        /// The default locale of the bot.
        /// </value>
        public string DefaultLocale { get; set; }

        /// <summary>
        /// Gets or sets the default voice font of the bot.
        /// </summary>
        /// <value>
        /// The default voice font of the bot.
        /// </value>
        public string VoiceFont { get; set; }

        /// <summary>
        /// Gets or sets the OAuth Connections for the bot.
        /// </summary>
        /// <value>
        /// The OAuth Connections for the bot.
        /// </value>
        public List<OAuthConnection> OAuthConnections { get; set; }

        /// <summary>
        /// Gets or sets the OAuthCredentials for OAuth.
        /// </summary>
        /// <value>
        /// The OAuthCredentials for OAuth for the bot.
        /// </value>
        public OAuthCredentialsConfiguration OAuthCredentials { get; set; }

        /// <summary>
        /// Gets or sets the CosmosDB Configuration for the bot.
        /// </summary>
        /// <value>
        /// The CosmosDB Configuration for the bot.
        /// </value>
        public CosmosDbPartitionedStorageOptions CosmosDb { get; set; }

        /// <summary>
        /// Gets or sets the Application Insights configuration for the bot.
        /// </summary>
        /// <value>
        /// The Application Insights configuration for the bot.
        /// </value>
        public TelemetryConfiguration AppInsights { get; set; }

        /// <summary>
        /// Gets or sets the Azure Blob Storage configuration for the bot.
        /// </summary>
        /// <value>
        /// The Azure Blob Storage configuration for the bot.
        /// </value>
        public BlobStorageConfiguration BlobStorage { get; set; }

        /// <summary>
        /// Gets or sets the Azure Content Moderator configuration for the bot.
        /// </summary>
        /// <value>
        /// The Azure Content Moderator configuration for the bot.
        /// </value>
        public ContentModeratorConfiguration ContentModerator { get; set; }

        /// <summary>
        /// Gets or sets the dictionary of cognitive model configurations by locale for the bot.
        /// </summary>
        /// <value>
        /// The dictionary of cognitive model configurations by locale for the bot.
        /// </value>
        public Dictionary<string, CognitiveModelConfiguration> CognitiveModels { get; set; } = new Dictionary<string, CognitiveModelConfiguration>();

        /// <summary>
        /// Gets or sets the Properties dictionary.
        /// </summary>
        /// <value>
        /// The Properties dictionary.
        /// </value>
        public Dictionary<string, string> Properties { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Class representing configuration for an Azure Blob Storage service.
        /// </summary>
        public class BlobStorageConfiguration
        {
            /// <summary>
            /// Gets or sets the connection string for the Azure Blob Storage service.
            /// </summary>
            /// <value>
            /// The connection string for the Azure Blob Storage service.
            /// </value>
            public string ConnectionString { get; set; }

            /// <summary>
            /// Gets or sets the blob container for the Azure Blob Storage service.
            /// </summary>
            /// <value>
            /// The blob container for the Azure Blob Storage service.
            /// </value>
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
            /// <value>
            /// The subscription key for the Content Moderator service.
            /// </value>
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
            /// <value>
            /// The Dispatch service for the set of cognitive models.
            /// </value>
            public LuisService DispatchModel { get; set; }

            /// <summary>
            /// Gets or sets the collection of LUIS models.
            /// </summary>
            /// <value>
            /// The collection of LUIS models.
            /// </value>
            public List<LuisService> LanguageModels { get; set; }

            /// <summary>
            /// Gets or sets the collection of QnA Maker knowledgebases.
            /// </summary>
            /// <value>
            /// The collection of QnA Maker knowledgebases.
            /// </value>
            // TODO #3139: Add required cognitive model class in Solutions SDK.
            public List<QnAMakerService> Knowledgebases { get; set; }
        }

        public class OAuthCredentialsConfiguration
        {
            /// <summary>
            /// Gets or sets the Microsoft App Id for OAuth.
            /// </summary>
            /// <value>
            /// The microsoft app id for OAuth.
            /// </value>
            public string MicrosoftAppId { get; set; }

            /// <summary>
            /// Gets or sets the Microsoft App Password for OAuth.
            /// </summary>
            /// <value>
            /// The microsoft app password for OAuth.
            /// </value>
            public string MicrosoftAppPassword { get; set; }
        }
    }
}