using AdaptiveAssistant.Services;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Adaptive;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Rules;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Steps;
using Microsoft.Bot.Builder.LanguageGeneration;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Globalization;

namespace AdaptiveAssistant.Dialogs
{
    public class AdaptiveMainDialog : ComponentDialog
    {
        private static IConfiguration Configuration;

        public AdaptiveMainDialog(IConfiguration configuration, BotServices services, TemplateEngine engine)
            : base(nameof(AdaptiveMainDialog))
        {
            Configuration = configuration;

            var localizedServices = services.CognitiveModelSets[CultureInfo.CurrentUICulture.TwoLetterISOLanguageName];

            var mainDialog = new AdaptiveDialog(nameof(AdaptiveDialog))
            {
                Recognizer = localizedServices.DispatchService,
                Generator = new TemplateEngineLanguageGenerator(nameof(AdaptiveMainDialog), engine),
                Rules = new List<IRule>()
                {
                    new EventRule()
                    {
                        Events = new List<string> { "activityReceived" },
                        Constraint = "turn.activity.type == 'conversationUpdate'",
                        Steps = new List<IDialog> { new SendActivity("Hi there!") }
                    },
                    new IntentRule("q_faq")
                    {
                        Steps = new List<IDialog>() { new SendActivity("faq") }
                    },
                    new IntentRule("q_chitchat")
                    {
                        Steps = new List<IDialog>() { new SendActivity("chitchat") }
                    },
                    new IntentRule("l_general")
                    {
                        Steps = new List<IDialog>(){ new BeginDialog(nameof(AdaptiveGeneralDialog)) }
                    },
                    new UnknownIntentRule()
                    {
                        Steps = new List<IDialog>
                        {
                            new TraceActivity(),
                            new SendActivity("[confusedMessage]")
                        }
                    }
                }
            };

            // Add named dialogs to the DialogSet. These names are saved in the dialog state.
            AddDialog(mainDialog);

            // Add all child dialogS
            AddDialog(new AdaptiveGeneralDialog(services, engine));

            // The initial child Dialog to run.
            InitialDialogId = nameof(AdaptiveDialog);
        }
    }
}
