// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.AI.Luis;
using Microsoft.Bot.Builder.AI.QnA;
using Microsoft.Bot.Builder.Solutions;
using Microsoft.Bot.Configuration;

namespace AdaptiveAssistant.Services
{
    public class BotServices
    {
        public BotServices(BotSettings settings, IBotTelemetryClient client)
        {
            foreach (var pair in settings.CognitiveModels)
            {
                var set = new CognitiveModelSet();
                var language = pair.Key;
                var config = pair.Value;

                var telemetryClient = client;
                var luisOptions = new LuisPredictionOptions()
                {
                    TelemetryClient = telemetryClient,
                    LogPersonalInformation = true,
                };

                var dispatchApp = new LuisApplication(config.DispatchModel.AppId, config.DispatchModel.SubscriptionKey, config.DispatchModel.GetEndpoint());

                set.DispatchRecognizer = new LuisRecognizer(dispatchApp, luisOptions);

                if (config.LanguageModels != null)
                {
                    foreach (var model in config.LanguageModels)
                    {
                        var luisApp = new LuisApplication(model.AppId, model.SubscriptionKey, model.GetEndpoint());
                        set.LuisRecognizers.Add(model.Id, new LuisRecognizer(luisApp, luisOptions));
                    }
                }

                foreach (var kb in config.Knowledgebases)
                {
                    var qnaEndpoint = new QnAMakerEndpoint()
                    {
                        KnowledgeBaseId = kb.KbId,
                        EndpointKey = kb.EndpointKey,
                        Host = kb.Hostname,
                    };

                    var qnaMaker = new QnAMaker(qnaEndpoint, null, null, telemetryClient: telemetryClient, logPersonalInformation: true);
                    set.QnAMakers.Add(kb.Id, qnaMaker);
                    set.QnAServices.Add(kb.Id, kb);
                }

                CognitiveModelSets.Add(language, set);
            }
        }

        public Dictionary<string, CognitiveModelSet> CognitiveModelSets { get; set; } = new Dictionary<string, CognitiveModelSet>();
    }
}