using AdaptiveAssistant.Services;
using Luis;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Adaptive;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Rules;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Steps;
using Microsoft.Bot.Builder.LanguageGeneration;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace AdaptiveAssistant.Dialogs
{
    public class AdaptiveGeneralDialog : ComponentDialog
    {
        public AdaptiveGeneralDialog(
            BotServices services,
            TemplateEngine engine)
            : base(nameof(AdaptiveGeneralDialog))
        {
            var localizedServices = services.CognitiveModelSets[CultureInfo.CurrentUICulture.TwoLetterISOLanguageName];

            var generalDialog = new AdaptiveDialog(nameof(AdaptiveDialog))
            {
                Recognizer = localizedServices.LuisServices["general"],
                Generator = new TemplateEngineLanguageGenerator(nameof(AdaptiveGeneralDialog), engine),
                Rules = new List<IRule>()
                {
                    new IntentRule(GeneralLuis.Intent.Cancel.ToString())
                    {
                        Steps = { new SendActivity("[cancelledMessage]") }
                    },
                    new IntentRule(GeneralLuis.Intent.Help.ToString())
                    {
                        Steps = { new SendActivity("[helpCard]") }
                    },
                    new IntentRule(GeneralLuis.Intent.Escalate.ToString())
                    {
                        Steps = { new BeginDialog(nameof(AdaptiveOnboardingDialog)), new SendActivity("[escalateMessage]") }
                    }
                }
            };

            AddDialog(generalDialog);
            AddDialog(new AdaptiveOnboardingDialog(engine));
            InitialDialogId = nameof(AdaptiveDialog);
        }
    }
}
