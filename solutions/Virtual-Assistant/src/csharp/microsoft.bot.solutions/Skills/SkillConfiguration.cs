using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Bot.Builder.AI.Luis;
using Microsoft.Bot.Builder.AI.QnA;
using Microsoft.Bot.Builder.Azure;
using Microsoft.Bot.Configuration;
using System.Collections.Generic;

namespace Microsoft.Bot.Solutions.Skills
{
    public class SkillConfiguration
    {
        public SkillConfiguration()
        {

        }

        public SkillConfiguration(BotConfiguration botConfiguration, string[] parameters, Dictionary<string, object> configuration)
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

                    case ServiceTypes.Luis:
                        {
                            var luis = service as LuisService;
                            var luisApp = new LuisApplication(luis.AppId, luis.SubscriptionKey, luis.GetEndpoint());
                            LuisServices.Add(service.Id, new LuisRecognizer(luisApp));
                            break;
                        }

                    case ServiceTypes.Generic:
                        {
                            if (service.Name == "Authentication")
                            {
                                var authentication = service as GenericService;

                                if (!string.IsNullOrEmpty(authentication.Configuration["Azure Active Directory v2"]))
                                {
                                    AuthConnectionName = authentication.Configuration["Azure Active Directory v2"];
                                }
                            }

                            break;
                        }
                }
            }

            // add the parameters the skill needs
            foreach (var parameter in parameters)
            {
                // Initialize each parameter to null. Needs to be set later by the bot.
                Properties.Add(parameter, null);
            }

            // add the additional keys the skill needs
            foreach (var set in configuration)
            {
                Properties.Add(set.Key, set.Value);
            }
        }

        public string AuthConnectionName { get; set; }

        public TelemetryClient TelemetryClient { get; set; }

        public CosmosDbStorageOptions CosmosDbOptions { get; set; }

        public Dictionary<string, LuisRecognizer> LuisServices { get; set; } = new Dictionary<string, LuisRecognizer>();

        public Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();
    }
}