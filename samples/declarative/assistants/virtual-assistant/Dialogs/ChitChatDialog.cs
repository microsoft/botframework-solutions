// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using VirtualAssistantSample.Services;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Adaptive;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Actions;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Conditions;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Generators;
using Microsoft.Bot.Builder.Dialogs.Adaptive.QnA.Recognizers;
using Microsoft.Bot.Solutions;
using Microsoft.Bot.Solutions.Responses;
using System;
using System.Globalization;
using Microsoft.Bot.Builder.AI.QnA;
using Microsoft.Bot.Builder.AI.QnA.Dialogs;

namespace VirtualAssistantSample.Dialogs
{
    public class ChitchatDialog : ComponentDialog
    {
        private const string KnowledgebaseId = "Chitchat";
        private readonly string DialogId = $"{nameof(ChitchatDialog)}.adaptive";

        public ChitchatDialog(
            BotServices botServices,
            MultiLanguageGenerator multiLanguageGenerator)
            : base(nameof(ChitchatDialog))
        { 
            var localizedServices = botServices.GetCognitiveModels();

            localizedServices.QnAConfiguration.TryGetValue("Chitchat", out QnAMakerEndpoint chitChatQnaMakerEndpoint);

            var chitchatDialog = new AdaptiveDialog(DialogId)
            {
                Triggers =
                {
                    new OnBeginDialog()
                    {
                        Actions =
                        {
                            new QnAMakerDialog(
                                knowledgeBaseId: chitChatQnaMakerEndpoint.KnowledgeBaseId,
                                endpointKey: chitChatQnaMakerEndpoint.EndpointKey,
                                hostName: chitChatQnaMakerEndpoint.Host),
                        }
                    }
                }
            };

            // TODO: Follow up with 400 error when using QnAMakerRecognizer
            //var chitchatDialog = new AdaptiveDialog(DialogId)
            //{
            //    Recognizer = GetQnAMakerRecognizer(KnowledgebaseId, localizedServices),
            //    Generator = multiLanguageGenerator,
            //    Triggers =
            //    {
            //        new OnQnAMatch()
            //        {
            //            Actions = 
            //            { 
            //                new TraceActivity(), 
            //                new SendActivity("Hi")}
            //        }
            //    }
            //};

            AddDialog(chitchatDialog);
        }

        private QnAMakerRecognizer GetQnAMakerRecognizer(string knowledgebaseId, CognitiveModelSet cognitiveModels)
        {
            if (!cognitiveModels.QnAConfiguration.TryGetValue(knowledgebaseId, out QnAMakerEndpoint qnaEndpoint)
                || qnaEndpoint == null)
            {
                throw new Exception($"Could not find QnA Maker knowledge base configuration with id: {knowledgebaseId}.");
            }

            return new QnAMakerRecognizer()
            {
                EndpointKey = qnaEndpoint.EndpointKey,
                HostName = qnaEndpoint.Host,
                KnowledgeBaseId = qnaEndpoint.KnowledgeBaseId
            };
        }
    }
}