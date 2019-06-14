using System.Collections.Generic;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Adaptive;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Input;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Recognizers;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Rules;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Steps;
using Microsoft.Bot.Builder.LanguageGeneration;

namespace AdaptiveAssistant.Dialogs
{
    public class AdaptiveOnboardingDialog : AdaptiveDialog
    {
        public AdaptiveOnboardingDialog(TemplateEngine engine)
            : base(nameof(AdaptiveOnboardingDialog))
        {
            Recognizer = new RegexRecognizer();
            Generator = new TemplateEngineLanguageGenerator(engine);

            Steps = new List<IDialog>()
            {
                new TextInput()
                {
                    Property = "user.name",
                    Prompt = new ActivityTemplate("[namePrompt]")
                },
                new SendActivity("[haveNameMessage]"),
                new TextInput()
                {
                    Property = "user.email",
                    Prompt = new ActivityTemplate("[emailPrompt]")
                },
                new SendActivity("[haveEmailMessage]"),
                new TextInput()
                {
                    Property = "user.location",
                    Prompt = new ActivityTemplate("[locationPrompt]")
                },
                new SendActivity("[haveLocationMessage]")
            };

            Rules = new List<IRule>()
            {
                new IntentRule("None")
            };
        }
    }
}