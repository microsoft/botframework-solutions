// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Bot.Builder.AI.Luis;
using Microsoft.Bot.Builder.AI.QnA;
using Microsoft.Bot.Builder.Azure;
using Microsoft.Bot.Configuration;
using Microsoft.Bot.Solutions.Middleware.Telemetry;
using Microsoft.Bot.Solutions.Skills;

namespace VirtualAssistant
{
    /// <summary>
    /// Represents references to external services.
    ///
    /// For example, LUIS services are kept here as a singleton.  This external service is configured
    /// using the <see cref="BotConfiguration"/> class.
    /// </summary>
    /// <seealso cref="https://docs.microsoft.com/en-us/aspnet/core/fundamentals/dependency-injection?view=aspnetcore-2.1"/>
    /// <seealso cref="https://www.luis.ai/home"/>
    public class BotServices
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BotServices"/> class.
        /// </summary>
        /// <param name="botConfiguration">The <see cref="BotConfiguration"/> instance for the bot.</param>
        /// <param name="skills">List of <see cref="SkillDefinition"/> for loading skill configurations.</param>
        /// <param name="languageModels">The locale specifc language model configs for each supported language.</param>
        public BotServices(BotConfiguration botConfiguration, Dictionary<string, Dictionary<string, string>> languageModels, List<SkillDefinition> skills)
        {
            // Create service clients for each service in the .bot file.
            foreach (var service in botConfiguration.Services)
            {
                switch (service.Type)
                {
                    case ServiceTypes.AppInsights:
                        {
                            var appInsights = (AppInsightsService)service;
                            if (appInsights == null)
                            {
                                throw new InvalidOperationException("The Application Insights is not configured correctly in your '.bot' file.");
                            }

                            if (string.IsNullOrWhiteSpace(appInsights.InstrumentationKey))
                            {
                                throw new InvalidOperationException("The Application Insights Instrumentation Key ('instrumentationKey') is required to run this sample.  Please update your '.bot' file.");
                            }

                            var telemetryConfig = new TelemetryConfiguration(appInsights.InstrumentationKey);
                            TelemetryClient = new TelemetryClient(telemetryConfig)
                            {
                                InstrumentationKey = appInsights.InstrumentationKey,
                            };

                            break;
                        }

                    case ServiceTypes.CosmosDB:
                        {
                            var cosmos = service as CosmosDbService;

                            CosmosDbOptions = new CosmosDbStorageOptions
                            {
                                AuthKey = cosmos.Key,
                                CollectionId = cosmos.Collection,
                                DatabaseId = cosmos.Database,
                                CosmosDBEndpoint = new Uri(cosmos.Endpoint),
                            };

                            break;
                        }

                    case ServiceTypes.Generic:
                        {
                            if (service.Name == "Authentication")
                            {
                                var authentication = service as GenericService;
                                AuthenticationConnections = authentication.Configuration;
                            }

                            break;
                        }
                }
            }

            // Create locale configuration object for each language config in appsettings.json
            foreach (var language in languageModels)
            {
                if (language.Value.TryGetValue("botFilePath", out var botFilePath) && File.Exists(botFilePath))
                {
                    var botFileSecret = language.Value["botFileSecret"];
                    var config = BotConfiguration.Load(botFilePath, !string.IsNullOrEmpty(botFileSecret) ? botFileSecret : null);

                    var localeConfig = new LocaleConfiguration
                    {
                        Locale = language.Key
                    };

                    foreach (var service in config.Services)
                    {
                        switch (service.Type)
                        {
                            case ServiceTypes.Dispatch:
                                {
                                    var dispatch = service as DispatchService;
                                    if (dispatch == null)
                                    {
                                        throw new InvalidOperationException("The Dispatch service is not configured correctly in your '.bot' file.");
                                    }

                                    if (string.IsNullOrWhiteSpace(dispatch.AppId))
                                    {
                                        throw new InvalidOperationException("The Dispatch Luis Model Application Id ('appId') is required to run this sample.  Please update your '.bot' file.");
                                    }

                                    if (string.IsNullOrWhiteSpace(dispatch.SubscriptionKey))
                                    {
                                        throw new InvalidOperationException("The Subscription Key ('subscriptionKey') is required to run this sample.  Please update your '.bot' file.");
                                    }

                                    var dispatchApp = new LuisApplication(dispatch.AppId, dispatch.SubscriptionKey, dispatch.GetEndpoint());
                                    localeConfig.DispatchRecognizer = new TelemetryLuisRecognizer(dispatchApp);
                                    break;
                                }

                            case ServiceTypes.Luis:
                                {
                                    var luis = service as LuisService;
                                    if (luis == null)
                                    {
                                        throw new InvalidOperationException("The Luis service is not configured correctly in your '.bot' file.");
                                    }

                                    if (string.IsNullOrWhiteSpace(luis.AppId))
                                    {
                                        throw new InvalidOperationException("The Luis Model Application Id ('appId') is required to run this sample.  Please update your '.bot' file.");
                                    }

                                    if (string.IsNullOrWhiteSpace(luis.AuthoringKey))
                                    {
                                        throw new InvalidOperationException("The Luis Authoring Key ('authoringKey') is required to run this sample.  Please update your '.bot' file.");
                                    }

                                    if (string.IsNullOrWhiteSpace(luis.SubscriptionKey))
                                    {
                                        throw new InvalidOperationException("The Subscription Key ('subscriptionKey') is required to run this sample.  Please update your '.bot' file.");
                                    }

                                    if (string.IsNullOrWhiteSpace(luis.Region))
                                    {
                                        throw new InvalidOperationException("The Region ('region') is required to run this sample.  Please update your '.bot' file.");
                                    }

                                    var luisApp = new LuisApplication(luis.AppId, luis.SubscriptionKey, luis.GetEndpoint());
                                    var recognizer = new TelemetryLuisRecognizer(luisApp);
                                    localeConfig.LuisServices.Add(service.Id, recognizer);
                                    break;
                                }

                            case ServiceTypes.QnA:
                                {
                                    var qna = service as QnAMakerService;
                                    var qnaEndpoint = new QnAMakerEndpoint()
                                    {
                                        KnowledgeBaseId = qna.KbId,
                                        EndpointKey = qna.EndpointKey,
                                        Host = qna.Hostname,
                                    };
                                    var qnaMaker = new TelemetryQnAMaker(qnaEndpoint);
                                    localeConfig.QnAServices.Add(qna.Id, qnaMaker);
                                    break;
                                }
                        }
                    }

                    LocaleConfigurations.Add(language.Key, localeConfig);
                }
            }

            // Create a skill configurations for each skill in appsettings.json
            foreach (var skill in skills)
            {
                var skillConfig = new SkillConfiguration()
                {
                    CosmosDbOptions = CosmosDbOptions
                };

                foreach (var localeConfig in LocaleConfigurations)
                {
                    skillConfig.LocaleConfigurations.Add(localeConfig.Key, new LocaleConfiguration
                    {
                        LuisServices = localeConfig.Value.LuisServices.Where(l => skill.LuisServiceIds.Contains(l.Key) == true).ToDictionary(l => l.Key, l => l.Value)
                    });
                }

                if (skill.SupportedProviders != null)
                {
                    foreach (var provider in skill.SupportedProviders)
                    {
                        var matches = AuthenticationConnections.Where(x => x.Value == provider);

                        foreach (var match in matches)
                        {
                            skillConfig.AuthenticationConnections.Add(match.Key, match.Value);
                        }
                    }
                }

                foreach (var set in skill.Configuration)
                {
                    skillConfig.Properties.Add(set.Key, set.Value);
                }

                SkillDefinitions.Add(skill);
                SkillConfigurations.Add(skill.Id, skillConfig);
            }
        }

        /// <summary>
        /// Gets the CosmosDb configuration used by the bot.
        /// </summary>
        /// <value>
        /// A <see cref="CosmosDbStorageOptions"/> instance created based on configuration in the .bot file.
        /// </value>
        public CosmosDbStorageOptions CosmosDbOptions { get; }

        /// <summary>
        /// Gets or sets the OAuth connections used by the bot.
        /// </summary>
        /// <value>
        /// Created based on the configuration of the Authentication generic service in the .bot file.
        /// The key for each item is the Connection Name, and the value is the OAuth provider.
        /// e.g. "Microsoft":"Azure Active Directory v2".
        /// </value>
        public Dictionary<string, string> AuthenticationConnections { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Gets the set of AppInsights Telemetry Client used.
        /// </summary>
        /// <remarks>The AppInsights Telemetry Client should not be modified while the bot is running.</remarks>
        /// <value>
        /// A <see cref="TelemetryClient"/> client instance created based on configuration in the .bot file.
        /// </value>
        public TelemetryClient TelemetryClient { get; }

        /// <summary>
        /// Gets or sets the cognitive model configurations for each locale.
        /// </summary>
        /// <value>
        /// Created based on the locale configuration .bot file(s) in the LocaleConfigurations folder.
        /// The key for each item is the two letter language code for the locale (e.g. "en").
        /// The value is a <see cref="LocaleConfiguration"/> containing localized Dispatch, LUIS, and QnA Maker service clients.
        /// </value>
        public Dictionary<string, LocaleConfiguration> LocaleConfigurations { get; set; } = new Dictionary<string, LocaleConfiguration>();

        /// <summary>
        /// Gets or sets the skill definitions for the bot.
        /// </summary>
        /// <value>
        /// Created based on the "skills" section of appSettings.json.
        /// Contains the information needed to invoke a skill.
        /// </value>
        public List<SkillDefinition> SkillDefinitions { get; set; } = new List<SkillDefinition>();

        /// <summary>
        /// Gets or sets the Skill Configurations for the bot.
        /// </summary>
        /// <value>
        /// Created based on the skill definitions from appsettings.json, the locale configurations, and shared bot services.
        /// The key for each item is the skill Id.
        /// The value is an <see cref="SkillConfigurationBase"/> object containing all the service clients used by the skill.
        /// </value>
        public Dictionary<string, SkillConfigurationBase> SkillConfigurations { get; set; } = new Dictionary<string, SkillConfigurationBase>();
    }
}