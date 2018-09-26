// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace ToDoSkill
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using global::ToDoSkill.Dialogs.Root;
    using global::ToDoSkill.ServiceClients;
    using Microsoft.Bot.Builder;
    using Microsoft.Bot.Builder.Dialogs;
    using Microsoft.Bot.Schema;

    /// <summary>
    /// Main entry point and orchestration for bot.
    /// </summary>
    public class ToDoSkill : IBot
    {
        /// <summary>
        /// Bot configuration object.
        /// </summary>
        private bool skillMode = false;
        private ToDoSkillAccessors toDoSkillAccessors;
        private ToDoSkillServices toDoSkillServices;
        private IToDoService toDoService;
        private DialogSet dialogs;

        /// <summary>
        /// Initializes a new instance of the <see cref="ToDoSkill"/> class.
        /// This constructor is used for Skill Activation, the parent Bot shouldn't have knowledge of the internal state workings so we fix this up here (rather than in startup.cs) in normal operation.
        /// </summary>
        /// <param name="botState">The Bot state.</param>
        /// <param name="stateName">The bot state name.</param>
        /// <param name="configuration">The configuration for the bot.</param>
        public ToDoSkill(BotState botState, string stateName = null, Dictionary<string, string> configuration = null)
        {
            // Flag that can be used for Skill specific behaviour (if needed)
            skillMode = true;
            toDoService = new ToDoService();
            toDoSkillServices = new ToDoSkillServices();

            // Create the properties and populate the Accessors. It's OK to call it DialogState as Skill mode creates an isolated area for this Skill so it doesn't conflict with Parent or other skills
            toDoSkillAccessors = new ToDoSkillAccessors
            {
                ToDoSkillState = botState.CreateProperty<ToDoSkillState>(stateName ?? nameof(ToDoSkillState)),
                ConversationDialogState = botState.CreateProperty<DialogState>("DialogState"),
            };

            // Initialise dialogs
            dialogs = new DialogSet(toDoSkillAccessors.ConversationDialogState);
            dialogs.Add(new RootDialog(skillMode, toDoSkillServices, toDoSkillAccessors, toDoService));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ToDoSkill"/> class.
        /// </summary>
        /// <param name="toDoSkillServices">To Do skill service.</param>
        /// <param name="toDoService">To Do provider service.</param>
        /// <param name="toDoSkillAccessors">To Do skill accessors.</param>
        public ToDoSkill(ToDoSkillServices toDoSkillServices, IToDoService toDoService, ToDoSkillAccessors toDoSkillAccessors)
        {
            this.toDoSkillAccessors = toDoSkillAccessors;
            this.toDoService = toDoService;
            dialogs = new DialogSet(this.toDoSkillAccessors.ConversationDialogState);
            this.toDoSkillServices = toDoSkillServices;

            // Initialise dialogs
            dialogs.Add(new RootDialog(skillMode, this.toDoSkillServices, this.toDoSkillAccessors, this.toDoService));
        }

        /// <summary>
        /// Run every turn of the conversation. Handles orchestration of messages.
        /// </summary>
        /// <param name="turnContext">Current turn context.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Completed Task.</returns>
        public async Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken)
        {
            var dc = await dialogs.CreateContextAsync(turnContext);
            var result = await dc.ContinueDialogAsync();
            if (result.Status == DialogTurnStatus.Empty)
            {
                if (!skillMode)
                {
                    // if localMode, check for conversation update from user before starting dialog
                    if (turnContext.Activity.Type == ActivityTypes.ConversationUpdate)
                    {
                        var activity = turnContext.Activity.AsConversationUpdateActivity();

                        // if conversation update is not from the bot.
                        if (!activity.MembersAdded.Any(m => m.Id == activity.Recipient.Id))
                        {
                            await dc.BeginDialogAsync(nameof(RootDialog));
                        }
                    }
                }
                else
                {
                    // if skillMode, begin dialog
                    await dc.BeginDialogAsync(nameof(RootDialog));
                }
            }
        }
    }
}