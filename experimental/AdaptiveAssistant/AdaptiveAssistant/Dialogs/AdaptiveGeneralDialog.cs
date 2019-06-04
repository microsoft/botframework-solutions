using AdaptiveAssistant.Services;
using Luis;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Adaptive;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Rules;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Steps;
using Microsoft.Bot.Builder.LanguageGeneration;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AdaptiveAssistant.Dialogs
{
    public class AdaptiveGeneralDialog : ComponentDialog
    {
        public AdaptiveGeneralDialog(BotServices services, TemplateEngine engine)
            : base(nameof(AdaptiveGeneralDialog))
        {
            var localizedServices = services.CognitiveModelSets[CultureInfo.CurrentUICulture.TwoLetterISOLanguageName];

            var generalDialog = new AdaptiveDialog(nameof(AdaptiveDialog))
            {
                Recognizer = localizedServices.LuisServices["general"],
                Generator = new TemplateEngineLanguageGenerator(nameof(AdaptiveGeneralDialog), engine),
                Rules = new List<IRule>()
                {
                    new IntentRule(
                        intent: GeneralLuis.Intent.Cancel.ToString(),
                        steps: new List<IDialog>
                        {
                            new TraceActivity(),
                            new SendActivity("[cancelledMessage]")
                        }),
                    new IntentRule(
                        intent: GeneralLuis.Intent.Help.ToString(),
                        steps: new List<IDialog>
                        {
                            new TraceActivity(),
                            new SendActivity("[helpCard]")
                        }),
                    new IntentRule(
                        intent: GeneralLuis.Intent.Escalate.ToString(),
                        steps: new List<IDialog>
                        {
                            new TraceActivity(),
                            new SendActivity("[escalateMessage]")
                        })
                }
            };

            AddDialog(generalDialog);
            InitialDialogId = nameof(AdaptiveDialog);
        }
    }
}
