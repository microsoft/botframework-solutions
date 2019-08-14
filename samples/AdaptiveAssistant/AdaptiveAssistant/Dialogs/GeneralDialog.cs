using AdaptiveAssistant.Services;
using Luis;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Adaptive;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Actions;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Events;
using Microsoft.Bot.Builder.LanguageGeneration;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace AdaptiveAssistant.Dialogs
{
    public class GeneralDialog : ComponentDialog
    {
        public GeneralDialog(
            BotSettings settings,
            BotServices services,
            TemplateEngine templateEngine)
            : base(nameof(DispatchDialog))
        {
            var localizedServices = services.CognitiveModelSets[CultureInfo.CurrentUICulture.TwoLetterISOLanguageName];

            var generalDialog = new AdaptiveDialog($"{nameof(GeneralDialog)}.adaptive")
            {
                Recognizer = localizedServices.LuisServices["General"],
                Generator = new TemplateEngineLanguageGenerator(templateEngine),
                Events = new List<IOnEvent>()
                {
                    new OnIntent(GeneralLuis.Intent.Help.ToString())
                    {
                        Actions = { new SendActivity("[helpCard]") },
                        Constraint = "turn.dialogevent.value.intents.Help.score > 0.5",
                    },
                    new OnIntent(GeneralLuis.Intent.Cancel.ToString())
                    {
                        Actions = {
                            new SendActivity("[cancelledMessage]"),
                            new CancelAllDialogs(),
                        },
                        Constraint = "turn.dialogevent.value.intents.Cancel.score > 0.5",
                    },
                    new OnIntent(GeneralLuis.Intent.Escalate.ToString())
                    {
                        Actions = { new SendActivity("[escalateCard]") }
                    },
                    new OnIntent(GeneralLuis.Intent.Logout.ToString())
                    {
                        Actions = { new SendActivity("[logoutMessage]") }
                    },
                    new OnUnknownIntent()
                    {
                        Actions = { new SendActivity("[confusedMessage]") }
                    }
                }
            };

            AddDialog(generalDialog);
        }
    }
}
