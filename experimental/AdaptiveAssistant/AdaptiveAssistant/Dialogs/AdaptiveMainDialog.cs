using AdaptiveAssistant.Services;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Adaptive;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Rules;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Selectors;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Steps;
using Microsoft.Bot.Builder.Expressions.Parser;
using System.Collections.Generic;
using System.Globalization;

namespace AdaptiveAssistant.Dialogs
{
    public class AdaptiveMainDialog : ComponentDialog
    {
        public AdaptiveMainDialog(BotServices services)
            : base(nameof(AdaptiveMainDialog))
        {
            var localizedServices = services.CognitiveModelSets[CultureInfo.CurrentUICulture.TwoLetterISOLanguageName];

            var mainDialog = new AdaptiveDialog()
            {
                AutoEndDialog = false,
                Selector = new MostSpecificSelector() { Selector = new FirstSelector() },
                Recognizer = localizedServices.DispatchService,
                Steps = new List<IDialog>()
                {
                    new IfCondition()
                    {
                        Condition = new ExpressionEngine().Parse("turn.activity.type == 'conversationUpdate'"),
                        Steps = new List<IDialog>
                        {
                            new SendActivity("[newUserIntroCard]")
                        }
                    }
                },
                Rules = new List<IRule>()
                {
                    new IntentRule(
                        intent: "l_general",
                        steps: new List<IDialog>
                        {
                            new TraceActivity(),
                            new BeginDialog(nameof(AdaptiveGeneralDialog))
                        }),
                    new IntentRule(
                        intent: "q_faq",
                        steps: new List<IDialog>
                        {
                            new TraceActivity(),
                            new SendActivity("FAQ triggered"),
                        }),
                    new IntentRule(
                        intent: "q_chitchat",
                        steps: new List<IDialog>
                        {
                            new TraceActivity(),
                            new SendActivity("Chitchat triggered"),
                        }),
                    new IntentRule(
                        intent: "None",
                        steps: new List<IDialog>
                        {
                            new TraceActivity(),
                            new SendActivity("[confusedMessage]")
                        }),
                    new UnknownIntentRule(
                        steps: new List<IDialog>
                        {
                            new TraceActivity(),
                            new SendActivity("[confusedMessage]")
                        })
                }
            };

            AddDialog(mainDialog);
            AddDialog(new AdaptiveGeneralDialog(services));
            InitialDialogId = mainDialog.Id;
        }
    }
}
