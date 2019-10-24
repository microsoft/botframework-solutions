using System.Collections.Generic;
using System.Globalization;
using AdaptiveAssistant.Actions;
using AdaptiveAssistant.Services;
using Luis;
using Microsoft.Bot.Builder.AI.QnA;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Adaptive;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Actions;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Conditions;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Generators;
using Microsoft.Bot.Builder.Skills;

namespace AdaptiveAssistant.Dialogs
{
    public class DispatchDialog : ComponentDialog
    {
        public DispatchDialog(
            BotSettings settings,
            BotServices services,
            List<SkillDialog> skillDialogs,
            GeneralDialog generalDialog,
            OnboardingDialog onboardingDialog)
            : base(nameof(DispatchDialog))
        {
            var localizedServices = services.CognitiveModelSets[CultureInfo.CurrentUICulture.TwoLetterISOLanguageName];

            var dispatchDialog = new AdaptiveDialog($"{nameof(DispatchDialog)}.adaptive")
            {
                Recognizer = localizedServices.DispatchService,
                Generator = new ResourceMultiLanguageGenerator("MainDialog.lg"),
                Triggers =
                {
                    new OnConversationUpdateActivity()
                    {
                        Actions =
                        {
                            new IfCondition()
                            {
                                Condition = "user.greeted == null",
                                Actions =
                                {
                                    new SendActivity("[newUserIntroCard]"),
                                    new SetProperty() { Property = "user.greeted", Value = "true" },
                                    new BeginDialog(onboardingDialog.Id)
                                },
                                ElseActions =
                                {
                                    new SendActivity("[returningUserIntroCard]"),
                                    new IfCondition()
                                    {
                                        Condition = "user.name == null",
                                        Actions = { new BeginDialog(onboardingDialog.Id) }
                                    }
                                }
                            }
                        }
                    },
                    new OnIntent(DispatchLuis.Intent.l_General.ToString())
                    {
                        Actions = { new BeginDialog(generalDialog.Id) }
                    },
                    new OnIntent(DispatchLuis.Intent.q_Chitchat.ToString())
                    {
                        Actions =
                        {
                            new TraceActivity(),
                            new QnAMakerDialog(
                                knowledgeBaseId: "settings.cognitiveModels.en.knowledgebases['0'].kbId",
                                endpointKey: "settings.cognitiveModels.en.knowledgebases['0'].endpointKey",
                                hostName: "settings.cognitiveModels.en.knowledgebases['0'].hostName",
                                threshold: 0)
                        }
                    },
                    new OnIntent(DispatchLuis.Intent.q_Faq.ToString())
                    {
                        Actions =
                        {
                            new TraceActivity(),
                            new QnAMakerDialog(
                                knowledgeBaseId: "settings.cognitiveModels.en.knowledgebases['1'].kbId",
                                endpointKey: "settings.cognitiveModels.en.knowledgebases['1'].endpointKey",
                                hostName: "settings.cognitiveModels.en.knowledgebases['1'].hostName",
                                threshold: 0)
                        }
                    },
                    new OnDialogEvent()
                    {
                        // If we recognized another intent, try to invoke a skill
                        Event = AdaptiveEvents.RecognizedIntent,
                        Actions = { new InvokeSkill(settings.Skills) }
                    },
                    new OnDialogEvent()
                    {
                        Event = DialogEvents.RepromptDialog,
                        Actions = { new SendActivity("[completedMessage]") }
                    },
                    new OnUnknownIntent()
                    {
                        Actions = { new SendActivity("[confusedMessage]") }
                    }
                }
            };

            AddDialog(dispatchDialog);
            AddDialog(generalDialog);
            AddDialog(onboardingDialog);

            foreach (var dialog in skillDialogs)
            {
                AddDialog(dialog);
            }
        }
    }
}
