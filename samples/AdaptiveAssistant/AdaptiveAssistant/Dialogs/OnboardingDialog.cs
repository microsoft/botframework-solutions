using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Adaptive;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Actions;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Events;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Input;
using Microsoft.Bot.Builder.LanguageGeneration;
using System.Collections.Generic;

namespace AdaptiveAssistant.Dialogs
{
    public class OnboardingDialog : ComponentDialog
    {
        public OnboardingDialog(TemplateEngine templateEngine)
            : base(nameof(OnboardingDialog))
        {
            var onboardingDialog = new AdaptiveDialog($"{nameof(OnboardingDialog)}.adaptive")
            {
                Generator = new TemplateEngineLanguageGenerator(templateEngine),
                Events =
                {
                    new OnBeginDialog()
                    {
                        Actions =
                        {
                            new TextInput()
                            {
                                Property = "user.name",
                                Prompt = new ActivityTemplate("[namePrompt]"),
                                AllowInterruptions = AllowInterruptions.Never
                            },
                            new SendActivity("[haveNameMessage]"),
                            //new TextInput()
                            //{
                            //    Property = "user.email",
                            //    Prompt = new ActivityTemplate("[emailPrompt]"),
                            //    AllowInterruptions = AllowInterruptions.Never
                            //},
                            //new SendActivity("[haveEmailMessage]"),
                            //new TextInput()
                            //{
                            //    Property = "user.location",
                            //    Prompt = new ActivityTemplate("[locationPrompt]"),
                            //    AllowInterruptions = AllowInterruptions.Never
                            //},
                            //new SendActivity("[haveLocationMessage]")
                        }
                    }
                }
            };

            AddDialog(onboardingDialog);
        }
    }
}
