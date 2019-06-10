using AdaptiveAssistant.Services;
using AdaptiveAssistant.Steps;
using Luis;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Adaptive;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Rules;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Steps;
using Microsoft.Bot.Builder.Expressions.Parser;
using Microsoft.Bot.Builder.LanguageGeneration;
using Microsoft.Bot.Builder.Skills;
using System.Collections.Generic;
using System.Globalization;

namespace AdaptiveAssistant.Dialogs
{
    public class AdaptiveMainDialog : ComponentDialog
    {
        public AdaptiveMainDialog(
            BotSettings settings,
            BotServices services,
            TemplateEngine engine,
            List<SkillDialog> skillDialogs)
            : base(nameof(AdaptiveMainDialog))
        {
            var localizedServices = services.CognitiveModelSets[CultureInfo.CurrentUICulture.TwoLetterISOLanguageName];

            var mainDialog = new AdaptiveDialog("mainAdaptive")
            {
                Recognizer = localizedServices.DispatchService,
                Generator = new TemplateEngineLanguageGenerator(nameof(AdaptiveMainDialog), engine),
                Rules =
                {
                    // Introduction event
                    new EventRule()
                    {
                        Events = { AdaptiveEvents.ActivityReceived },
                        Constraint = "turn.activity.type == 'conversationUpdate'",
                        Steps =
                        {
                            // If user has not been greeted, show newUserIntroCard, else show returningUserIntroCard
                            new IfCondition()
                            {
                                Condition = new ExpressionEngine().Parse("user.greeted == null"),
                                Steps =
                                {
                                    new SendActivity("[newUserIntroCard]"),
                                    new SetProperty() { Property = "user.greeted", Value = new ExpressionEngine().Parse("true") },
                                },
                                ElseSteps = { new SendActivity("Welcome back!") }
                            },
                            // If we do not have the user's name, start the onboarding dialog
                            new IfCondition()
                            {
                                Condition = new ExpressionEngine().Parse("user.name == null"),
                                Steps = { new BeginDialog(nameof(AdaptiveOnboardingDialog)) }
                            }
                        }
                    },
                    // General intents (Cancel, Help, Escalate, etc)
                    new IntentRule(DispatchLuis.Intent.l_general.ToString())
                    {
                        Steps = { new BeginDialog(nameof(AdaptiveGeneralDialog)) }
                    },
                    // FAQ QnA Maker
                    new IntentRule(DispatchLuis.Intent.q_faq.ToString())
                    {
                        Steps = { new InvokeQnAMaker(localizedServices.QnAServices["faq"]) }
                    },
                    // Chitchat QnA Maker
                    new IntentRule(DispatchLuis.Intent.q_chitchat.ToString())
                    {
                        Steps = { new InvokeQnAMaker(localizedServices.QnAServices["chitchat"]) }
                    },
                    // Check unhandled identified intents agains registered skills
                    new EventRule()
                    {
                        Events = { AdaptiveEvents.RecognizedIntent },
                        Steps = { new InvokeSkill(settings) }
                    },
                    // If a QnA intent was triggered, but no match was found
                    new EventRule()
                    {
                        Events = { "NoQnAMatch" },
                        Steps = { new SendActivity("[confusedMessage]") }
                    },
                    new UnknownIntentRule()
                    {
                        Steps = { new SendActivity("[confusedMessage]") }
                    }
                }
            };

            // Add named dialogs to the DialogSet. These names are saved in the dialog state.
            AddDialog(mainDialog);

            // Add all child dialogs
            AddDialog(new AdaptiveGeneralDialog(services, engine));
            AddDialog(new AdaptiveOnboardingDialog(engine));

            foreach (var dialog in skillDialogs)
            {
                AddDialog(dialog);
            }

            // The initial child Dialog to run.
            InitialDialogId = "mainAdaptive";
        }
    }
}