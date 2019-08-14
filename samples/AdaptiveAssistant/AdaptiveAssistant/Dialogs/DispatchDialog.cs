using AdaptiveAssistant.Actions;
using AdaptiveAssistant.Services;
using Luis;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Adaptive;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Actions;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Events;
using Microsoft.Bot.Builder.LanguageGeneration;
using Microsoft.Bot.Builder.Skills;
using System.Collections.Generic;
using System.Globalization;

namespace AdaptiveAssistant.Dialogs
{
    public class DispatchDialog : ComponentDialog
    {
        public DispatchDialog(
            BotSettings settings,
            BotServices services,
            TemplateEngine templateEngine,
            List<SkillDialog> skillDialogs,
            GeneralDialog generalDialog,
            OnboardingDialog onboardingDialog)
            : base(nameof(DispatchDialog))
        {
            var localizedServices = services.CognitiveModelSets[CultureInfo.CurrentUICulture.TwoLetterISOLanguageName];

            var dispatchDialog = new AdaptiveDialog($"{nameof(DispatchDialog)}.adaptive")
            {
                Recognizer = localizedServices.DispatchService,
                Generator = new TemplateEngineLanguageGenerator(templateEngine),
                Events = new List<IOnEvent>()
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
                        Actions = { new CallQnAMaker(localizedServices.QnAServices["Chitchat"])}
                    },
                    new OnIntent(DispatchLuis.Intent.q_Faq.ToString())
                    {
                        Actions = { new CallQnAMaker(localizedServices.QnAServices["Faq"])}
                    },
                    new OnDialogEvent()
                    {
                        // If we recognized another intent, try to invoke a skill
                        Events = { AdaptiveEvents.RecognizedIntent },
                        Actions = { new InvokeSkill(settings.Skills) }
                    },
                    new OnDialogEvent()
                    {
                        Events = { "NoQnAMatch" },
                        Actions = { new SendActivity("[confusedMessage]") }
                    },
                    new OnDialogEvent()
                    {
                        Events = { AdaptiveEvents.EndDialog },
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
