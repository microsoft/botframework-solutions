// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using VirtualAssistantSample.Services;
using Luis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.AI.QnA;
using Microsoft.Bot.Builder.AI.QnA.Dialogs;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Adaptive;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Actions;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Conditions;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Generators;
using Microsoft.Bot.Builder.Dialogs.Adaptive.QnA.Recognizers;
using Microsoft.Bot.Solutions;
using Microsoft.Bot.Solutions.Responses;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;

namespace VirtualAssistantSample.Dialogs
{
    /// <summary>
    /// This is an example root dialog. Replace this with your applications.
    /// </summary>
    public class RootDialog : ComponentDialog
    {
        private readonly BotServices _botServices;
        private readonly LocaleTemplateManager _localeTemplateManager;
        private readonly string DialogId = $"{nameof(RootDialog)}.adaptive";

        private static OnboardingDialog _onboardingDialog;

        public RootDialog(
            IServiceProvider serviceProvider,
            ChitchatDialog chitchatDialog,
            FaqDialog faqDialog,
            GeneralDialog generalDialog,
            OnboardingDialog onboardingDialog)
            : base(nameof(RootDialog))
        {
            _botServices = serviceProvider.GetService<BotServices>();
            _localeTemplateManager = serviceProvider.GetService<LocaleTemplateManager>();
            _onboardingDialog = onboardingDialog;

            var localizedServices = _botServices.GetCognitiveModels();
            var localizedTemplateEngine = _localeTemplateManager.GetTemplates();

            var dispatchDialog = new AdaptiveDialog(DialogId)
            {
                Recognizer = localizedServices.DispatchService,
                Generator = new TemplateEngineLanguageGenerator(localizedTemplateEngine),
                Triggers =
                {
                    // Greet user added to the conversation
                    new OnConversationUpdateActivity()
                    {
                        Actions = WelcomeUserSteps()
                    },
                    new OnIntent(DispatchLuis.Intent.l_General.ToString())
                    {
                        Actions = 
                        {
                            new BeginDialog(generalDialog.Id)
                            {
                                ActivityProcessed = false
                            }
                        }
                    },
                    new OnIntent(DispatchLuis.Intent.q_Faq.ToString())
                    {
                        Actions = 
                        {
                            new BeginDialog(faqDialog.Id)
                            {
                                ActivityProcessed = false
                            }
                        }
                    },
                    new OnDialogEvent()
                    {
                        Event = AdaptiveEvents.RecognizedIntent,
                        Actions = { new SkillDialog(new SkillDialogOptions())}
                    },
                    new OnUnknownIntent()
                    {
                        Actions =
                        {
                            new BeginDialog(chitchatDialog.Id)
                            {
                                ActivityProcessed = false
                            }
                        }
                    }
                }
            };

            // Add named dialogs to the DialogSet. These names are saved in the dialog state.
            AddDialog(dispatchDialog);
            AddDialog(chitchatDialog);
            AddDialog(faqDialog);
            AddDialog(generalDialog);
            AddDialog(onboardingDialog);

            // The initial child Dialog to run.
            InitialDialogId = DialogId;
        }

        private static List<Dialog> WelcomeUserSteps()
        {
            return new List<Dialog>()
            {
                // Iterate through membersAdded list and greet user added to the conversation.
                new Foreach()
                {
                    ItemsProperty = "turn.activity.membersAdded",
                    Actions = new List<Dialog>()
                    {
                        // Note: Some channels send two conversation update events - one for the Bot added to the conversation and another for user.
                        // Filter cases where the bot itself is the recipient of the message. 
                        new IfCondition()
                        {
                            Condition = "$foreach.value.name != turn.activity.recipient.name",
                            Actions = new List<Dialog>()
                            {
                                new IfCondition()
                                {
                                    Condition = "user.greeted == null",
                                    Actions =
                                    {
                                        new SendActivity("${NewUserIntroCard()}"),
                                        new SetProperty() { Property = "user.greeted", Value = "true" }
                                    },
                                    ElseActions =
                                    {
                                        new SendActivity("${ReturningUserIntroCard()}")
                                    }
                                }
                            }
                        }
                    }
                }
            };
        }
    }
}
