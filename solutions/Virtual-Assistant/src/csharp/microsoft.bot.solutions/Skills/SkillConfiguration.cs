using System.Collections.Generic;
using System.Linq;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.AI.Luis;
using Microsoft.Bot.Builder.Azure;
using Microsoft.Bot.Configuration;

namespace Microsoft.Bot.Solutions.Skills
{
    public class SkillConfiguration : ISkillConfiguration
    {
        public SkillConfiguration()
        {
        }

        public SkillConfiguration(BotConfiguration botConfiguration, string[] supportedProviders, string[] parameters, Dictionary<string, object> configuration)
        {
            if (supportedProviders != null && supportedProviders.Count() > 0)
            {
                IsAuthenticatedSkill = true;
            }

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

                    case ServiceTypes.Luis:
                        {
                            var luis = service as LuisService;
                            var luisApp = new LuisApplication(luis.AppId, luis.SubscriptionKey, luis.GetEndpoint());
                            LuisServices.Add(service.Id, new TelemetryLuisRecognizer(luisApp));
                            break;
                        }

                    case ServiceTypes.Generic:
                        {
                            if (service.Name == "Authentication")
                            {
                                var auth = service as GenericService;

                                foreach (var provider in supportedProviders)
                                {
                                    auth.Configuration.TryGetValue(provider, out var connectionName);

                                    if (connectionName != null)
                                    {
                                        AuthenticationConnections.Add(provider, connectionName);
                                    }
                                }
                            }

                            break;
                        }
                }
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

        public override TelemetryClient TelemetryClient { get; set; }

        public override CosmosDbStorageOptions CosmosDbOptions { get; set; }

        public override Dictionary<string, IRecognizer> LuisServices { get; set; } = new Dictionary<string, IRecognizer>();

        public override Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();
    }
}