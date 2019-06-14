using AdaptiveAssistant.Services;
using Luis;
using Microsoft.Bot.Builder.Dialogs.Adaptive;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Rules;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Steps;
using Microsoft.Bot.Builder.LanguageGeneration;
using System.Collections.Generic;
using System.Globalization;

namespace AdaptiveAssistant.Dialogs
{
    public class AdaptiveGeneralDialog : AdaptiveDialog
    {
        public AdaptiveGeneralDialog(
            BotServices services,
            TemplateEngine engine)
            : base(nameof(AdaptiveGeneralDialog))
        {
            var localizedServices = services.CognitiveModelSets[CultureInfo.CurrentUICulture.TwoLetterISOLanguageName];

            Recognizer = localizedServices.LuisServices["general"];
            Generator = new TemplateEngineLanguageGenerator(engine);
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
                    Steps = { new SendActivity("[escalateMessage]") }
                },
                new IntentRule("None")
            };
        }
    }
}
