using AdaptiveAssistant.Services;
using AdaptiveAssistant.Steps;
using Luis;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Adaptive;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Input;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Rules;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Steps;
using Microsoft.Bot.Builder.Expressions.Parser;
using Microsoft.Bot.Builder.LanguageGeneration;
using System.Globalization;

namespace AdaptiveAssistant.Dialogs
{
    public class AdaptiveMainDialog : ComponentDialog
    {
        public AdaptiveMainDialog(
            BotServices services,
            TemplateEngine engine)
            : base(nameof(AdaptiveMainDialog))
        {
            var localizedServices = services.CognitiveModelSets[CultureInfo.CurrentUICulture.TwoLetterISOLanguageName];

            var mainDialog = new AdaptiveDialog(nameof(AdaptiveDialog))
            {
                Recognizer = localizedServices.DispatchService,
                Generator = new TemplateEngineLanguageGenerator(nameof(AdaptiveMainDialog), engine),
                Rules =
                {
                    new EventRule()
                    {
                        Events = { "activityReceived" },
                        Constraint = "turn.activity.type == 'conversationUpdate'",
                        Steps =
                        {
                            new IfCondition()
                            {
                                Condition = new ExpressionEngine().Parse("user.greeted == null"),
                                Steps = { new SendActivity("[newUserIntroCard]") },
                                ElseSteps = { new SendActivity("[returningUserIntroCard]") }
                            },
                        }
                    },
                    new IntentRule(DispatchLuis.Intent.l_general.ToString())
                    {
                        Steps = { new BeginDialog(nameof(AdaptiveGeneralDialog)) }
                    },
                    new IntentRule(DispatchLuis.Intent.q_faq.ToString())
                    {
                        Steps = { new CallQnAMaker(localizedServices.QnAServices["faq"]) }
                    },
                    new IntentRule(DispatchLuis.Intent.q_chitchat.ToString())
                    {
                        Steps = { new CallQnAMaker(localizedServices.QnAServices["chitchat"]) }
                    },
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

            // The initial child Dialog to run.
            InitialDialogId = nameof(AdaptiveDialog);
        }
    }
}


//AutoEndDialog = false,
//Steps = new List<IDialog>
//{
//    new IfCondition(){
//        Condition = new ExpressionEngine().Parse("turn.activity.type == 'conversationUpdate'"),
//        Steps = new List<IDialog>
//        {
//            new SendActivity("Hi there!")
//        }
//    }
//},

//new EventRule()
//{
//    Events = new List<string>{ AdaptiveEvents.RecognizedIntent },
//    Steps = new List<IDialog>
//    {
//        new CodeStep(async (dc, result) =>
//        {
//           return EndOfTurn;
//        })
//    }
//},