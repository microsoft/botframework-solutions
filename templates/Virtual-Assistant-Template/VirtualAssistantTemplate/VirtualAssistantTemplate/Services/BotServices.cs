﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using Microsoft.Bot.Builder.AI.Luis;
using Microsoft.Bot.Builder.AI.QnA;
using Microsoft.Bot.Builder.Solutions;
using Microsoft.Bot.Builder.Solutions.Telemetry;

namespace VirtualAssistantTemplate.Services
{
    public class BotServices
    {
        public BotServices()
        {
        }

        public BotServices(BotSettings settings)
        {
            foreach (var pair in settings.CognitiveModels)
            {
                var set = new CognitiveModelSet();
                var language = pair.Key;
                var config = pair.Value;

                var dispatchApp = new LuisApplication(config.DispatchModel.AppId, config.DispatchModel.SubscriptionKey, config.DispatchModel.GetEndpoint());
                set.DispatchService = new TelemetryLuisRecognizer(dispatchApp);

                if (config.LanguageModels != null)
                {
                    foreach (var model in config.LanguageModels)
                    {
                        var luisApp = new LuisApplication(model.AppId, model.SubscriptionKey, model.GetEndpoint());
                        set.LuisServices.Add(model.Id, new TelemetryLuisRecognizer(luisApp));
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
                    var qnaMaker = new TelemetryQnAMaker(qnaEndpoint);
                    set.QnAServices.Add(kb.Id, qnaMaker);
                }

                CognitiveModelSets.Add(language, set);
            }
        }

        public Dictionary<string, CognitiveModelSet> CognitiveModelSets { get; set; } = new Dictionary<string, CognitiveModelSet>();
    }
}