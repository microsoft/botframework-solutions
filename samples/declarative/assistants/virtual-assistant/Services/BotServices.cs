// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.AI.Luis;
using Microsoft.Bot.Builder.AI.QnA;
using Microsoft.Bot.Builder.Dialogs.Adaptive.QnA.Recognizers;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Recognizers;
using Microsoft.Bot.Solutions;

namespace VirtualAssistantSample.Services
{
    public class BotServices
    {
        public BotServices()
        {
        }

        public BotServices(BotSettings settings, IBotTelemetryClient client)
        {
            foreach (var pair in settings.CognitiveModels)
            {
                var set = new AdaptiveCognitiveModelSet();
                var language = pair.Key;
                var config = pair.Value;

                var telemetryClient = client;

                LuisRecognizerOptionsV3 luisOptions;

                if (config.DispatchModel != null)
                {
                    var dispatchApp = new LuisAdaptiveRecognizer(){
                        ApplicationId = config.DispatchModel.AppId, 
                        EndpointKey = config.DispatchModel.SubscriptionKey,
                        Endpoint = config.DispatchModel.GetEndpoint(),
                        TelemetryClient = telemetryClient,
                        LogPersonalInformation = true,
                    };

                    set.DispatchService = dispatchApp;
                }

                if (config.LanguageModels != null)
                {
                    foreach (var model in config.LanguageModels)
                    {                        
                        var luisApp = new LuisAdaptiveRecognizer()
                        {
                            ApplicationId = model.AppId,
                            EndpointKey = model.SubscriptionKey,
                            Endpoint = model.GetEndpoint(),
                            TelemetryClient = telemetryClient,
                            LogPersonalInformation = true,
                        };
                        
                        set.LuisServices.Add(model.Id, luisApp);
                    }
                }

                foreach (var kb in config.Knowledgebases)
                {
                    var qnaEndpoint = new QnAMakerEndpoint()
                    {
                        KnowledgeBaseId = kb.KbId,
                        EndpointKey = kb.EndpointKey,
                        Host = kb.Hostname
                    };

                    set.QnAConfiguration.Add(kb.Id, qnaEndpoint);
                }

                CognitiveModelSets.Add(language, set);
            }
        }

        public Dictionary<string, AdaptiveCognitiveModelSet> CognitiveModelSets { get; set; } = new Dictionary<string, AdaptiveCognitiveModelSet>();

        public AdaptiveCognitiveModelSet GetCognitiveModels()
        {
            // Get cognitive models for locale
            var locale = CultureInfo.CurrentUICulture.Name.ToLower();

            var cognitiveModel = CognitiveModelSets.ContainsKey(locale)
                ? CognitiveModelSets[locale]
                : CognitiveModelSets.Where(key => key.Key.StartsWith(locale.Substring(0, 2))).FirstOrDefault().Value
                ?? throw new Exception($"There's no matching locale for '{locale}' or its root language '{locale.Substring(0, 2)}'. " +
                                        "Please review your available locales in your cognitivemodels.json file.");

            return cognitiveModel;
        }
    }
}