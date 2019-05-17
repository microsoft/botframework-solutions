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
    public class SendEmailAdaptiveDialog : ComponentDialog
    {
        public SendEmailAdaptiveDialog(SendEmailDialog sendEmailDialog)
            : base(nameof(SendEmailAdaptiveDialog))
        {
            var skillOptions = new EmailSkillDialogOptions
            {
                SubFlowMode = false
            };

            var rootDialog = new AdaptiveDialog("SendEmailAdaptiveDialog1")
            {
                // Create a LUIS recognizer.
                // The recognizer is built using the intents, utterances, patterns and entities defined in ./RootDialog.lu file
                Recognizer = CreateRecognizer(),
                Rules = new List<IRule>()
                {
                    // Intent rules for the LUIS model. Each intent here corresponds to an intent defined in ./Dialogs/Resources/ToDoBot.lu file
                    new IntentRule("None") { Steps = new List<IDialog>() { } },
                    // Since we are using a regex recognizer, anything except for help or cancel will come back as none intent.
                    // If so, just accept user's response as the title of the todo and move forward.
                    //new IntentRule("None")
                    //{
                    //    Steps = new List<IDialog>()
                    //    {
                    //    }
                    //}
                },
                Steps = new List<IDialog>()
                {
                    new BeginDialog(nameof(SendEmailDialog), options: skillOptions)
                },
            };

            AddDialog(rootDialog);
            rootDialog.AddDialog(new List<IDialog>() { sendEmailDialog });
            AddDialog(sendEmailDialog ?? throw new ArgumentNullException(nameof(sendEmailDialog)));

            InitialDialogId = "SendEmailAdaptiveDialog1";
        }

        private static IRecognizer CreateRecognizer()
        {
            return new LuisRecognizer(new LuisApplication()
            {
                Endpoint = "https://westus.api.cognitive.microsoft.com/luis/v2.0/apps/5578c91d-fa3c-468d-af86-50eecd52ff9d?verbose=true&timezoneOffset=-360&subscription-key=fa24469556fe41caa1a0119741cbf280&q=",//Configuration["LuisAPIHostName"],
                EndpointKey = "fa24469556fe41caa1a0119741cbf280",
                ApplicationId = "5578c91d-fa3c-468d-af86-50eecd52ff9d",
            });
        }
    }
}
