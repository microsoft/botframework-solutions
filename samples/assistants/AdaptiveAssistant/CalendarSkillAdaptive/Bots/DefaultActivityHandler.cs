// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.AI.Luis;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Adaptive;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Actions;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Conditions;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Input;
using Microsoft.Bot.Builder.LanguageGeneration.Templates;

namespace CalendarSkillAdaptive
{
    public class DefaultActivityHandler : ActivityHandler
   {
        private DialogManager _dialogManager;

        public DefaultActivityHandler()
        {
            var createDialog = new AdaptiveDialog("create")
            {
                Recognizer = new LuisRecognizer(new LuisApplication(applicationId: "149e2684-86de-4f6b-af9c-26905a866bf3", endpointKey: "28a816a21d184f2e8b116ceb7db71b26", endpoint: "https://westus.api.cognitive.microsoft.com")),
                Triggers = new List<OnCondition>
                {
                    new OnBeginDialog()
                    {
                        Actions =
                        {
                            new TextInput()
                            {
                                Property = "dialog.title",
                                Prompt = new ActivityTemplate("What's the meeting title?"),
                                AllowInterruptions = AllowInterruptions.Always
                            },
                            new TextInput()
                            {
                                Property = "dialog.content",
                                Prompt = new ActivityTemplate("What's the meeting content?"),
                                AllowInterruptions = AllowInterruptions.Always
                            },
                        }
                    },
                    new OnDialogEvent()
                    {
                        Event = AdaptiveEvents.RecognizedIntent,
                        Actions = { new SendActivity("recognized") }
                    }
                }
            };
            var mainAdaptive = new AdaptiveDialog("root")
            {
                Recognizer = new LuisRecognizer(new LuisApplication(applicationId: "149e2684-86de-4f6b-af9c-26905a866bf3", endpointKey: "28a816a21d184f2e8b116ceb7db71b26", endpoint: "https://westus.api.cognitive.microsoft.com")),
                Triggers = new List<OnCondition>
                {
                    new OnIntent("ScheduleMeeting")
                    {
                        Actions = { new BeginDialog(createDialog.Id) }
                    }
                },
            };

            mainAdaptive.Dialogs.Add(createDialog);

            _dialogManager = new DialogManager(mainAdaptive);
        }

        public override Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken = default(CancellationToken))
        {
            return _dialogManager.OnTurnAsync(turnContext, cancellationToken: cancellationToken);
        }
    }
}
