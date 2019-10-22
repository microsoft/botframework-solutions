using System.Globalization;
using AdaptiveAssistant.Services;
using Luis;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Adaptive;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Actions;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Conditions;
using Microsoft.Bot.Builder.LanguageGeneration.Generators;

namespace AdaptiveAssistant.Dialogs
{
    public class GeneralDialog : ComponentDialog
    {
        public GeneralDialog(BotServices services)
            : base(nameof(DispatchDialog))
        {
            var localizedServices = services.CognitiveModelSets[CultureInfo.CurrentUICulture.TwoLetterISOLanguageName];

            var generalDialog = new AdaptiveDialog($"{nameof(GeneralDialog)}.adaptive")
            {
                Recognizer = localizedServices.LuisRecognizers["General"],
                Generator = new ResourceMultiLanguageGenerator("MainDialog.lg"),
                Triggers =
                {
                    new OnIntent(GeneralLuis.Intent.Help.ToString())
                    {
                        Actions = { new SendActivity("[helpCard]") },
                        Condition = "turn.dialogevent.value.intents.Help.score > 0.5",
                    },
                    new OnIntent(GeneralLuis.Intent.Cancel.ToString())
                    {
                        Actions = {
                            new SendActivity("[cancelledMessage]"),
                            new CancelAllDialogs(),
                        },
                        Condition = "turn.dialogevent.value.intents.Cancel.score > 0.5",
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
