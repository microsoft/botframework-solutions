// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions.Skills;

namespace DemoSkill
{
    /// <summary>
    /// Main entry point and orchestration for bot.
    /// </summary>
    public class DemoSkill : IBot
    {
        private bool _skillMode = false;
        private DemoSkillAccessors _accessors;
        private DemoSkillResponses _responder;
        private DemoSkillServices _services;
        private DialogSet _dialogs;

        // Skill Mode Constructor
        public DemoSkill(BotState botState, string stateName = null, Dictionary<string, string> configuration = null)
        {
            // Flag that can be used for Skill specific behaviour (if needed)
            _skillMode = true;

            // Create the properties and populate the Accessors. It's OK to call it DialogState as Skill mode creates an isolated area for this Skill so it doesn't conflict with Parent or other skills
            _accessors = new DemoSkillAccessors
            {
                DemoSkillState = botState.CreateProperty<DemoSkillState>(stateName ?? nameof(DemoSkillState)),
                ConversationDialogState = botState.CreateProperty<DialogState>("DialogState"),
            };

            if (configuration != null)
            {
                // If LUIS configuration data is passed then this Skill needs to have LUIS available for use internally
                string luisAppId;
                string luisSubscriptionKey;
                string luisEndpoint;

                configuration.TryGetValue("LuisAppId", out luisAppId);
                configuration.TryGetValue("LuisSubscriptionKey", out luisSubscriptionKey);
                configuration.TryGetValue("LuisEndpoint", out luisEndpoint);

                ////if (!string.IsNullOrEmpty(luisAppId) && !string.IsNullOrEmpty(luisSubscriptionKey) && !string.IsNullOrEmpty(luisEndpoint))
                ////{
                ////    LuisApplication luisApplication = new LuisApplication(luisAppId, luisSubscriptionKey, luisEndpoint);

                ////    _services = new DemoSkillServices();
                ////    _services.LuisRecognizer = new Microsoft.Bot.Builder.AI.Luis.LuisRecognizer(luisApplication);
                ////}
            }

            _dialogs = new DialogSet(_accessors.ConversationDialogState);
            _responder = new DemoSkillResponses();

            RegisterDialogs();
        }

        public DemoSkill(DemoSkillServices services, DemoSkillAccessors accessors)
        {
            _accessors = accessors;
            _dialogs = new DialogSet(accessors.ConversationDialogState);
            _responder = new DemoSkillResponses();
            _services = services;

            RegisterDialogs();
        }

        public async Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken)
        {
            // Get the conversation state from the turn context
            var state = await _accessors.DemoSkillState.GetAsync(turnContext, () => new DemoSkillState());
            var dialogState = await _accessors.ConversationDialogState.GetAsync(turnContext, () => new DialogState());

            if (turnContext.Activity.Type == ActivityTypes.Message)
            {
                var dc = await _dialogs.CreateContextAsync(turnContext);

                // If an active dialog is waiting, continue dialog
                var result = await dc.ContinueDialogAsync();

                var skillOptions = new DemoSkillDialogOptions
                {
                    SkillMode = _skillMode,
                };

                switch (result.Status)
                {
                    case DialogTurnStatus.Empty:
                        {
                            await dc.BeginDialogAsync(ProfileDialog.Name, skillOptions);
                            break;
                        }

                    case DialogTurnStatus.Complete:
                        {
                            // if the dialog is complete, send endofconversation to complete the skill
                            var response = turnContext.Activity.CreateReply();
                            response.Type = ActivityTypes.EndOfConversation;

                            await turnContext.SendActivityAsync(response);
                            await dc.EndDialogAsync();
                            break;
                        }

                    default:
                        {
                            await _responder.ReplyWith(turnContext, DemoSkillResponses.Confused);
                            break;
                        }
                }
            }
            else if (turnContext.Activity.Type == ActivityTypes.Event)
            {
                await HandleEventMessage(turnContext);
            }
            else
            {
                await HandleSystemMessage(turnContext);
            }
        }

        private async Task HandleEventMessage(ITurnContext turnContext)
        {
            if (turnContext.Activity.Name == "skillBegin")
            {
                var state = await _accessors.DemoSkillState.GetAsync(turnContext, () => new DemoSkillState());
                SkillMetadata skillMetadata = turnContext.Activity.Value as SkillMetadata;
                if (skillMetadata != null)
                {
                    // .Configuration has any configuration settings required for operation
                    // .Parameters has any user information configured to be passed
                }
            }
            else if (turnContext.Activity.Name == "tokens/response")
            {
                // Auth dialog completion
                var dialogContext = await _dialogs.CreateContextAsync(turnContext);
                var result = await dialogContext.ContinueDialogAsync();

                // If the dialog completed when we sent the token, end the skill conversation
                if (result.Status != DialogTurnStatus.Waiting)
                {
                    var response = turnContext.Activity.CreateReply();
                    response.Type = ActivityTypes.EndOfConversation;

                    await turnContext.SendActivityAsync(response);
                }
            }
        }

        private async Task HandleSystemMessage(ITurnContext turnContext)
        {
            switch (turnContext.Activity.Type)
            {
                case ActivityTypes.ConversationUpdate:
                    {
                        // greet when added to conversation
                        var activity = turnContext.Activity.AsConversationUpdateActivity();
                        if (activity.MembersAdded.Where(m => m.Id == activity.Recipient.Id).Any())
                        {
                            await _responder.ReplyWith(turnContext, DemoSkillResponses.Intro);
                        }
                    }

                    break;
                case ActivityTypes.ContactRelationUpdate:
                case ActivityTypes.DeleteUserData:
                case ActivityTypes.EndOfConversation:
                case ActivityTypes.Typing:
                    break;
            }
        }

        private void RegisterDialogs()
        {
            _dialogs.Add(new ProfileDialog());
        }
    }
}