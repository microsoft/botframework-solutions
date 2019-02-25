using System.Collections.Generic;
using System.Linq;
using Microsoft.Bot.Builder.AI.Luis;
using Microsoft.Bot.Builder.AI.QnA;
using Microsoft.Bot.Builder.Azure;
using Microsoft.Bot.Configuration;
using Microsoft.Bot.Solutions.Middleware.Telemetry;

namespace Microsoft.Bot.Solutions.Skills
{
    public class SkillConfiguration : SkillConfigurationBase
    {
        public SkillConfiguration()
        {
        }

        public SkillConfiguration(BotConfiguration botConfiguration, Dictionary<string, Dictionary<string, string>> languageModels, string[] supportedProviders = null, string[] parameters = null, Dictionary<string, object> configuration = null)
        {
            if (supportedProviders != null && supportedProviders.Count() > 0)
            {
                IsAuthenticatedSkill = true;
            }

            foreach (var service in botConfiguration.Services)
            {
                switch (service.Type)
                {
                    case ServiceTypes.Generic:
                        {
                            if (service.Name == "Authentication")
                            {
                                var auth = service as GenericService;

                                foreach (var provider in supportedProviders)
                                {
                                    var matches = auth.Configuration.Where(x => x.Value == provider);

                                    foreach (var match in matches)
                                    {
                                        AuthenticationConnections.Add(match.Key, match.Value);
                                    }
                                }
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

            if (parameters != null)
            {
                // add the parameters the skill needs
                foreach (var parameter in parameters)
                {
                    // Initialize each parameter to null. Needs to be set later by the bot.
                    Properties.Add(parameter, null);
                }
            }

            if (configuration != null)
            {
                // add the additional keys the skill needs
                foreach (var set in configuration)
                {
                    Properties.Add(set.Key, set.Value);
                }
            }
        }

        public override Dictionary<string, string> AuthenticationConnections { get; set; } = new Dictionary<string, string>();

        public override CosmosDbStorageOptions CosmosDbOptions { get; set; }

        public override Dictionary<string, LocaleConfiguration> LocaleConfigurations { get; set; } = new Dictionary<string, LocaleConfiguration>();

        public override Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();
    }
}