﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Solutions.Skills;
using ToDoSkill.Dialogs.Main;
using ToDoSkill.ServiceClients;

namespace ToDoSkill
{
    /// <summary>
    /// Main entry point and orchestration for bot.
    /// </summary>
    public class ToDoSkill : IBot
    {
        private readonly SkillConfigurationBase _services;
        private readonly ConversationState _conversationState;
        private readonly UserState _userState;
        private readonly IBotTelemetryClient _telemetryClient;
        private IServiceManager _serviceManager;
        private DialogSet _dialogs;
        private bool _skillMode;

        public ToDoSkill(SkillConfigurationBase services, ConversationState conversationState, UserState userState, IBotTelemetryClient telemetryClient, bool skillMode = false, IServiceManager serviceManager = null)
        {
            _skillMode = skillMode;
            _services = services ?? throw new ArgumentNullException(nameof(services));
            _userState = userState ?? throw new ArgumentNullException(nameof(userState));
            _conversationState = conversationState ?? throw new ArgumentNullException(nameof(conversationState));
            _telemetryClient = telemetryClient ?? throw new ArgumentNullException(nameof(telemetryClient));
            _serviceManager = serviceManager ?? new ServiceManager();

            _dialogs = new DialogSet(_conversationState.CreateProperty<DialogState>(nameof(DialogState)));
            _dialogs.Add(new MainDialog(_services, _conversationState, _userState, _telemetryClient, _serviceManager, _skillMode));
        }

        /// <summary>
        /// Run every turn of the conversation. Handles orchestration of messages.
        /// </summary>
        /// <param name="turnContext">Bot Turn Context.</param>
        /// <param name="cancellationToken">Task CancellationToken.</param>
        /// <returns>A <see cref="TaskItem"/> representing the asynchronous operation.</returns>
        public async Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken)
        {
            var dc = await _dialogs.CreateContextAsync(turnContext);

            if (dc.ActiveDialog != null)
            {
                var result = await dc.ContinueDialogAsync();
            }
            else
            {
                await dc.BeginDialogAsync(nameof(MainDialog));
            }
        }
    }
}