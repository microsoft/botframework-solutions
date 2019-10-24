using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Adaptive;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Actions;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Conditions;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Generators;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Input;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Templates;

namespace AdaptiveAssistant.Dialogs
{
    public class OnboardingDialog : ComponentDialog
    {
        public OnboardingDialog()
            : base(nameof(OnboardingDialog))
        {
            var onboardingDialog = new AdaptiveDialog($"{nameof(OnboardingDialog)}.adaptive")
            {
                Generator = new ResourceMultiLanguageGenerator("OnboardingDialog.lg"),
                Triggers =
                {
                    new OnBeginDialog()
                    {
                        Actions =
                        {
                            new TextInput()
                            {
                                Property = "user.name",
                                Prompt = new ActivityTemplate("[namePrompt]"),
                                AllowInterruptions = "false"
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
