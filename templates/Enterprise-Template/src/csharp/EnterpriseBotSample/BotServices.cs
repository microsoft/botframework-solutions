// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using EnterpriseBotSample.Middleware.Telemetry;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Bot.Builder.AI.Luis;
using Microsoft.Bot.Builder.AI.QnA;
using Microsoft.Bot.Configuration;

namespace EnterpriseBotSample
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
        public BotServices()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BotServices"/> class.
        /// </summary>
        /// <param name="botConfiguration">The <see cref="BotConfiguration"/> instance for the bot.</param>
        public BotServices(BotConfiguration botConfiguration)
        {
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
                            DispatchRecognizer = new TelemetryLuisRecognizer(dispatchApp);
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
                            LuisServices.Add(service.Id, recognizer);
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
                            QnAServices.Add(qna.Id, qnaMaker);
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
        }

        /// <summary>
        /// Gets the set of the Authentication Connection Name for the Bot application.
        /// </summary>
        /// <remarks>The Authentication Connection Name  should not be modified while the bot is running.</remarks>
        /// <value>
        /// A string based on configuration in the .bot file.
        /// </value>
        public string AuthConnectionName { get; set; }

        /// <summary>
        /// Gets the set of AppInsights Telemetry Client used.
        /// </summary>
        /// <remarks>The AppInsights Telemetry Client should not be modified while the bot is running.</remarks>
        /// <value>
        /// A <see cref="TelemetryClient"/> client instance created based on configuration in the .bot file.
        /// </value>
        public TelemetryClient TelemetryClient { get; set; }

        /// <summary>
        /// Gets the set of Dispatch LUIS Recognizer used.
        /// </summary>
        /// <remarks>The Dispatch LUIS Recognizer should not be modified while the bot is running.</remarks>
        /// <value>
        /// A <see cref="LuisRecognizer"/> client instance created based on configuration in the .bot file.
        /// </value>
        public ITelemetryLuisRecognizer DispatchRecognizer { get; set; }

        /// <summary>
        /// Gets the set of LUIS Services used.
        /// Given there can be multiple <see cref="TelemetryLuisRecognizer"/> services used in a single bot,
        /// LuisServices is represented as a dictionary.  This is also modeled in the
        /// ".bot" file since the elements are named.
        /// </summary>
        /// <remarks>The LUIS services collection should not be modified while the bot is running.</remarks>
        /// <value>
        /// A <see cref="LuisRecognizer"/> client instance created based on configuration in the .bot file.
        /// </value>
        public Dictionary<string, ITelemetryLuisRecognizer> LuisServices { get; set; } = new Dictionary<string, ITelemetryLuisRecognizer>();

        /// <summary>
        /// Gets the set of QnAMaker Services used.
        /// Given there can be multiple <see cref="TelemetryQnAMaker"/> services used in a single bot,
        /// QnAServices is represented as a dictionary.  This is also modeled in the
        /// ".bot" file since the elements are named.
        /// </summary>
        /// <remarks>The QnAMaker services collection should not be modified while the bot is running.</remarks>
        /// <value>
        /// A <see cref="ITelemetryQnAMaker"/> client instance created based on configuration in the .bot file.
        /// </value>
        public Dictionary<string, ITelemetryQnAMaker> QnAServices { get; set; } = new Dictionary<string, ITelemetryQnAMaker>();
    }
}
