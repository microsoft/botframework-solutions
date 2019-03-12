﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Solutions.Proactive;
using Microsoft.Bot.Builder.Solutions.Responses;
using Microsoft.Bot.Builder.Solutions.Skills;
using Microsoft.Bot.Builder.Solutions.TaskExtensions;
using Microsoft.Bot.Configuration;
using Microsoft.Bot.Schema;

namespace NewsSkill
{
    /// <summary>
    /// Main entry point and orchestration for bot.
    /// </summary>
    public class NewsSkill : IBot
    {
        private readonly SkillConfigurationBase _services;
        private readonly EndpointService _endpointService;
        private readonly ConversationState _conversationState;
        private readonly IBotTelemetryClient _telemetryClient;
        private readonly UserState _userState;
        private DialogSet _dialogs;

        private bool _skillMode;

        /// <summary>
        /// Initializes a new instance of the <see cref="NewsSkill"/> class.
        /// </summary>
        /// <param name="services">Skill Configuration information.</param>
        /// <param name="endpointService">Endpoint service for the bot.</param>
        /// <param name="conversationState">Conversation State.</param>
        /// <param name="userState">User State.</param>
        /// <param name="proactiveState">Proative state.</param>
        /// <param name="telemetryClient">Telemetry Client.</param>
        /// <param name="backgroundTaskQueue">Background task queue.</param>
        /// <param name="serviceManager">Service Manager.</param>
        /// <param name="skillMode">Indicates whether the skill is running in skill or local mode.</param>
        public NewsSkill(
            SkillConfigurationBase services,
            EndpointService endpointService,
            ConversationState conversationState,
            UserState userState,
            ProactiveState proactiveState,
            IBotTelemetryClient telemetryClient,
            IBackgroundTaskQueue backgroundTaskQueue,
            bool skillMode = false)
        {
            _skillMode = skillMode;
            _services = services ?? throw new ArgumentNullException(nameof(services));
            _endpointService = endpointService ?? throw new ArgumentNullException(nameof(endpointService));
            _userState = userState ?? throw new ArgumentNullException(nameof(userState));

            _conversationState = conversationState ?? throw new ArgumentNullException(nameof(conversationState));
            _telemetryClient = telemetryClient ?? throw new ArgumentNullException(nameof(telemetryClient));

            _dialogs = new DialogSet(_conversationState.CreateProperty<DialogState>(nameof(DialogState)));
            _dialogs.Add(new MainDialog(_services, _conversationState, _userState, _telemetryClient, _skillMode));
        }

        /// <summary>
        /// Run every turn of the conversation. Handles orchestration of messages.
        /// </summary>
        /// <param name="turnContext">Bot Turn Context.</param>
        /// <param name="cancellationToken">Task CancellationToken.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken)
        {
            var dc = await _dialogs.CreateContextAsync(turnContext);
            var result = await dc.ContinueDialogAsync();

            if (result.Status == DialogTurnStatus.Empty)
            {
                if (!_skillMode)
                {
                    // if localMode, check for conversation update from user before starting dialog
                    if (turnContext.Activity.Type == ActivityTypes.ConversationUpdate)
                    {
                        var activity = turnContext.Activity.AsConversationUpdateActivity();

                        // if conversation update is not from the bot.
                        if (!activity.MembersAdded.Any(m => m.Id == activity.Recipient.Id))
                        {
                            await dc.BeginDialogAsync(nameof(MainDialog));
                        }
                    }
                }
                else
                {
                    // if skillMode, begin dialog
                    await dc.BeginDialogAsync(nameof(MainDialog));
                }
            }
        }
    }
}