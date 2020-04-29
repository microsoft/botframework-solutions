using VirtualAssistantSample.Services;
using Luis;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Adaptive;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Actions;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Conditions;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Generators;
using Microsoft.Bot.Builder.Dialogs.Adaptive.QnA.Recognizers;
using Microsoft.Bot.Solutions;
using Microsoft.Bot.Solutions.Responses;
using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Input;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Recognizers;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Templates;

namespace VirtualAssistantSample.Dialogs
{
    public class OnboardingDialog : ComponentDialog
    {
        private readonly string DialogId = $"{nameof(OnboardingDialog)}.adaptive";

        public OnboardingDialog(
            BotServices botServices,
            MultiLanguageGenerator multiLanguageGenerator)
            : base(nameof(OnboardingDialog))
        {
            var onboardingDialog = new AdaptiveDialog(DialogId)
            {
                Generator = multiLanguageGenerator,
                Triggers =
                {
                    new OnBeginDialog()
                    {
                        Actions =
                        {
                            new TextInput()
                            {
                                Property = "user.name",
                                Prompt = new ActivityTemplate("${NamePrompt()}"),
                                AllowInterruptions = "false"
                            },
                            new SendActivity("${HaveNameMessage()}")
                        }
                    }
                }
            };

            AddDialog(onboardingDialog);
        }
    }
}
