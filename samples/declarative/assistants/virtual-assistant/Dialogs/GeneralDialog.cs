// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using VirtualAssistantSample.Services;
using Luis;
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
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos.Linq;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.AI.QnA;
using Microsoft.Bot.Builder.AI.QnA.Dialogs;
using Microsoft.Bot.Builder.Teams;

namespace VirtualAssistantSample.Dialogs
{
    public class GeneralDialog : ComponentDialog
    {
        private const string LanguageModelId = "General";
        private readonly string DialogId = $"{nameof(GeneralDialog)}.adaptive";

        public GeneralDialog(
            BotServices botServices,
            MultiLanguageGenerator multiLanguageGenerator,
            ChitchatDialog chitchatDialog)
            : base(nameof(GeneralDialog))
        {
            var localizedServices = botServices.GetCognitiveModels();

            var generalDialog = new AdaptiveDialog(DialogId)
            {
                Recognizer = localizedServices.LuisServices[LanguageModelId],
                Generator = multiLanguageGenerator,
                Triggers =
                {
                    new OnIntent(GeneralLuis.Intent.Cancel.ToString())
                    {
                        Actions =
                        {
                            new SendActivity("${CancelledMessage()}"),
                            new CancelAllDialogs(),
                        },
                        Condition = "turn.recognized.score > 0.5",
                    },
                    new OnIntent(GeneralLuis.Intent.Escalate.ToString())
                    {
                        Actions =
                        {
                            new SendActivity("${EscalateMessage()}"),
                            // TODO: Include sample Human Handoff implementation
                        },
                        Condition = "turn.recognized.score > 0.5",
                    },
                    new OnIntent(GeneralLuis.Intent.Help.ToString())
                    {
                        Actions =
                        {
                            new SendActivity("${HelpCard()}")
                        },
                        Condition = "turn.recognized.score > 0.5",
                    },
                    new OnIntent(GeneralLuis.Intent.Logout.ToString())
                    {
                        Actions =
                        {
                            // TODO: Validate sign out user method
                            //new CodeAction(LogUserOutAsync),
                            //new SignOutUser()
                            //{
                            //},
                            new SendActivity("${LogoutMessage()}"), 
                            new CancelAllDialogs()
                        },
                        Condition = "turn.recognized.score > 0.5",
                    },
                    new OnIntent(GeneralLuis.Intent.Repeat.ToString())
                    {
                        Actions =
                        {
                            // TODO: Send the previous set of bot activities
                        },
                        Condition = "turn.recognized.score > 0.5",
                    },
                    new OnIntent(GeneralLuis.Intent.StartOver.ToString())
                    {
                        Actions =
                        {
                            new SendActivity("${StartOverMessage()}"),
                            // TODO: Start over previous dialog, will test with RepeatDialog() but likely have to use ReplaceDialog() with last dialog stored
                            new RepeatDialog(),
                        },
                        Condition = "turn.recognized.score > 0.5",
                    },
                    new OnUnknownIntent()
                    {
                        Actions =
                        {
                            new BeginDialog(chitchatDialog.Id)
                            {
                                ActivityProcessed = false
                            }
                        },
                        Condition = "turn.recognized.score < 0.5",
                    },
                }
            };

            AddDialog(generalDialog);
        }
    }
}