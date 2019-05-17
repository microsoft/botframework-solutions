using EmailSkill.Models;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.AI.Luis;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Adaptive;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Rules;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Steps;
using System;
using System.Collections.Generic;

namespace EmailSkill.Dialogs
{
    public class ShowEmailAdaptiveDialog : ComponentDialog
    {
        public ShowEmailAdaptiveDialog(ShowEmailDialog showEmailDialog)
            : base(nameof(ShowEmailAdaptiveDialog))
        {
            var skillOptions = new EmailSkillDialogOptions
            {
                SubFlowMode = false
            };

            var rootDialog = new AdaptiveDialog(nameof(AdaptiveDialog))
            {
                // Create a LUIS recognizer.
                // The recognizer is built using the intents, utterances, patterns and entities defined in ./RootDialog.lu file
                Recognizer = CreateRecognizer(),
                Rules = new List<IRule>()
                {
                    // Intent rules for the LUIS model. Each intent here corresponds to an intent defined in ./Dialogs/Resources/ToDoBot.lu file
                    new UnknownIntentRule() { Steps = new List<IDialog>() { new SendActivity("This is none intent") } }
                },
                Steps = new List<IDialog>()
                {
                    new BeginDialog(nameof(ShowEmailDialog), options: skillOptions)
                },
            };

            rootDialog.AddDialog(new List<IDialog>() { showEmailDialog });
            AddDialog(showEmailDialog ?? throw new ArgumentNullException(nameof(showEmailDialog)));
        }

        private static IRecognizer CreateRecognizer()
        {
            return new LuisRecognizer(new LuisApplication()
            {
                Endpoint = "https://westus.api.cognitive.microsoft.com/luis/v2.0/apps/1a441c29-5a3f-4615-9e6c-7473ffaa815c?verbose=true&timezoneOffset=-360&subscription-key=fa24469556fe41caa1a0119741cbf280&q=",
                EndpointKey = "fa24469556fe41caa1a0119741cbf280",
                ApplicationId = "1a441c29-5a3f-4615-9e6c-7473ffaa815c",
            });
        }
    }
}
