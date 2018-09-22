// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.AI.Luis;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;

namespace EmailSkill
{
    /// <summary>
    /// Main entry point and orchestration for bot.
    /// </summary>
    public class EmailSkill : IBot
    {
        private bool _skillMode = false;
        private EmailSkillAccessors _accessors;
        private EmailSkillServices _services;
        private IMailSkillServiceManager _serviceManager;
        private DialogSet _dialogs;

        /// <summary>
        /// Initializes a new instance of the <see cref="EmailSkill"/> class.
        /// This constructor is used for Skill Activation, the parent Bot shouldn't have knowledge of the internal state workings so we fix this up here (rather than in startup.cs) in normal operation.
        /// </summary>
        /// <param name="botState">The Bot state.</param>
        /// <param name="stateName">The bot state name.</param>
        /// <param name="configuration">Services configuration.</param>
        public EmailSkill(BotState botState, string stateName = null, Dictionary<string, string> configuration = null)
        {
            // Flag that can be used for Skill specific behaviour (if needed)
            this._skillMode = true;
            this._serviceManager = new MailSkillServiceManager();

            // Create the properties and populate the Accessors. It's OK to call it DialogState as Skill mode creates an isolated area for this Skill so it doesn't conflict with Parent or other skills
            this._accessors = new EmailSkillAccessors
            {
                EmailSkillState = botState.CreateProperty<EmailSkillState>(stateName ?? nameof(EmailSkillState)),
                ConversationDialogState = botState.CreateProperty<DialogState>("DialogState"),
            };

            // Initialise dialogs.
            this._dialogs = new DialogSet(this._accessors.ConversationDialogState);
            if (configuration != null)
            {
                configuration.TryGetValue("LuisAppId", out var luisAppId);
                configuration.TryGetValue("LuisSubscriptionKey", out var luisSubscriptionKey);
                configuration.TryGetValue("LuisEndpoint", out var luisEndpoint);

                if (!string.IsNullOrEmpty(luisAppId) && !string.IsNullOrEmpty(luisSubscriptionKey) && !string.IsNullOrEmpty(luisEndpoint))
                {
                    var luisApplication = new LuisApplication(luisAppId, luisSubscriptionKey, luisEndpoint);

                    this._services = new EmailSkillServices()
                    {
                        LuisRecognizer = new LuisRecognizer(luisApplication),
                    };
                }
            }

            this._dialogs.Add(new RootDialog(this._skillMode, this._services, this._accessors, this._serviceManager));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EmailSkill"/> class.
        /// Initializes new instance of EnterpriseBot class - standard constructor for normal Bot invocation.
        /// </summary>
        /// <param name="services">The email skill services.</param>
        /// <param name="emailSkillStateAccessors">The email bot state object.</param>
        /// <param name="serviceManager">The service manager inject into flow to search user and email, etc.</param>
        public EmailSkill(EmailSkillServices services, EmailSkillAccessors emailSkillStateAccessors, IMailSkillServiceManager serviceManager)
        {
            this._accessors = emailSkillStateAccessors;
            this._serviceManager = serviceManager;
            this._dialogs = new DialogSet(this._accessors.ConversationDialogState);
            this._services = services;

            this._dialogs.Add(new RootDialog(this._skillMode, this._services, this._accessors, this._serviceManager));
        }

        /// <summary>
        /// Run every turn of the conversation. Handles orchestration of messages.
        /// </summary>
        /// <param name="turnContext">Current turn context.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Completed Task.</returns>
        public async Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken)
        {
            var dc = await this._dialogs.CreateContextAsync(turnContext);
            var result = await dc.ContinueDialogAsync();

            if (result.Status == DialogTurnStatus.Empty)
            {
                if (!this._skillMode)
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