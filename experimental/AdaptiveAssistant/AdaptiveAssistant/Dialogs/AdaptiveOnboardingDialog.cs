using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Adaptive;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Input;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Recognizers;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Rules;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Steps;
using Microsoft.Bot.Builder.Expressions.Parser;
using Microsoft.Bot.Builder.LanguageGeneration;

namespace AdaptiveAssistant.Dialogs
{
    public class AdaptiveOnboardingDialog : ComponentDialog
    {
        public AdaptiveOnboardingDialog(TemplateEngine engine)
            : base(nameof(AdaptiveOnboardingDialog))
        {
            var dialog = new AdaptiveDialog(nameof(AdaptiveDialog))
            {
                Generator = new TemplateEngineLanguageGenerator(nameof(AdaptiveOnboardingDialog), engine),
                Steps = new List<IDialog>()
                {
                    new TextInput()
                    {
                        Property = "turn.name",
                        Prompt = new ActivityTemplate("[namePrompt]")
                    },
                    new SendActivity("[haveNameMessage]")
                }
            };

            AddDialog(dialog);
            InitialDialogId = nameof(AdaptiveDialog);
        }
    }
}