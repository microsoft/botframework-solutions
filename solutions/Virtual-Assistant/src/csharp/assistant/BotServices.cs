// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Bot.Builder.AI.Luis;
using Microsoft.Bot.Builder.AI.QnA;
using Microsoft.Bot.Builder.Azure;
using Microsoft.Bot.Configuration;
using Microsoft.Bot.Solutions;
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
            foreach (var service in botConfiguration.Services)
            {
                switch (service.Type)
                {
                    case ServiceTypes.AppInsights:
                        {
                            var appInsights = service as AppInsightsService;
                            var telemetryConfig = new TelemetryConfiguration(appInsights.InstrumentationKey);
                            TelemetryClient = new TelemetryClient(telemetryConfig);
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

            foreach (var language in languageModels)
            {
                var localeConfig = new LocaleConfiguration
                {
                    Locale = language.Key
                };

                var path = language.Value["botFilePath"];
                var secret = language.Value["botFileSecret"];
                var config = BotConfiguration.Load(path, !string.IsNullOrEmpty(secret) ? secret : null);

                foreach (var service in config.Services)
                {
                    switch (service.Type)
                    {
                        case ServiceTypes.Dispatch:
                            {
                                var dispatch = service as DispatchService;
                                var dispatchApp = new LuisApplication(dispatch.AppId, dispatch.SubscriptionKey, dispatch.GetEndpoint());
                                localeConfig.DispatchRecognizer = new TelemetryLuisRecognizer(dispatchApp);
                                break;
                            }

                        case ServiceTypes.Luis:
                            {
                                var luis = service as LuisService;
                                var luisApp = new LuisApplication(luis.AppId, luis.SubscriptionKey, luis.GetEndpoint());
                                localeConfig.LuisServices.Add(service.Id, new TelemetryLuisRecognizer(luisApp));
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

            foreach (var skill in skills)
            {
                var skillConfig = new SkillConfiguration()
                {
                    CosmosDbOptions = CosmosDbOptions,
                    TelemetryClient = TelemetryClient,
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

        public CosmosDbStorageOptions CosmosDbOptions { get; }

        public Dictionary<string, string> AuthenticationConnections { get; set; } = new Dictionary<string, string>();

        public TelemetryClient TelemetryClient { get; }

        public Dictionary<string, LocaleConfiguration> LocaleConfigurations { get; set; } = new Dictionary<string, LocaleConfiguration>();

        public List<SkillDefinition> SkillDefinitions { get; set; } = new List<SkillDefinition>();

        public Dictionary<string, ISkillConfiguration> SkillConfigurations { get; set; } = new Dictionary<string, ISkillConfiguration>();
    }
}
