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
using System.Collections.Generic;
using System.Globalization;
using Microsoft.Bot.Builder.AI.QnA;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Templates;
using Microsoft.Bot.Builder.AI.QnA.Dialogs;

namespace VirtualAssistantSample.Dialogs
{
    public class FaqDialog : ComponentDialog
    {
        private const string KnowledgebaseId = "Faq";
        private readonly string DialogId = $"{nameof(FaqDialog)}.adaptive";

        public FaqDialog(
            BotServices botServices,
            MultiLanguageGenerator multiLanguageGenerator)
            : base(nameof(FaqDialog))
        { 
            var localizedServices = botServices.GetCognitiveModels();
            localizedServices.QnAConfiguration.TryGetValue("Faq", out QnAMakerEndpoint faqQnAMakerEndpoint);

            //var faqDialog = new AdaptiveDialog(DialogId)
            //{
            //    Triggers =
            //    {
            //        new OnBeginDialog()
            //        {
            //            Actions =
            //            {
            //                new QnAMakerDialog(
            //                    knowledgeBaseId: faqQnAMakerEndpoint.KnowledgeBaseId,
            //                    endpointKey: faqQnAMakerEndpoint.EndpointKey,
            //                    hostName: faqQnAMakerEndpoint.Host),
            //            }
            //        }
            //    }
            //};

            // TODO: Follow up with 400 error when using QnAMakerRecognizer
            var faqDialog = new AdaptiveDialog(DialogId)
            {
                Recognizer = GetQnAMakerRecognizer(KnowledgebaseId, localizedServices),
                Generator = multiLanguageGenerator,
                Triggers =
                {
                    new OnQnAMatch()
                    {
                        Actions = new List<Dialog>()
                        {
                            new SendActivity()
                            {
                                Activity = new ActivityTemplate("${@answer}")
                            }
                        }
                    },
                    new OnUnknownIntent()
                    {
                        Actions = new List<Dialog>()
                        {
                            new SendActivity("Wha?")
                        }
                    }
                }
            };

            AddDialog(faqDialog);
        }

        private QnAMakerRecognizer GetQnAMakerRecognizer(string knowledgebaseId, AdaptiveCognitiveModelSet cognitiveModels)
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