// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Configuration;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions.Models.Proactive;
using Microsoft.Bot.Solutions.TaskExtensions;
using VirtualAssistant.Dialogs.Main;

namespace VirtualAssistant
{
    /// <summary>
    /// Main entry point and orchestration for bot.
    /// </summary>
    public class VirtualAssistant : IBot
    {
        private readonly BotServices _services;
        private readonly ConversationState _conversationState;
        private readonly UserState _userState;
        private readonly ProactiveState _proactiveState;
        private readonly EndpointService _endpointService;
        private readonly IBotTelemetryClient _telemetryClient;
        private readonly IBackgroundTaskQueue _backgroundTaskQueue;
        private DialogSet _dialogs;

        /// <summary>
        /// Initializes a new instance of the <see cref="VirtualAssistant"/> class.
        /// </summary>
        /// <param name="botServices">Bot services.</param>
        /// <param name="conversationState">Bot conversation state.</param>
        /// <param name="userState">Bot user state.</param>
        /// <param name="proactiveState">Proactive state.</param>
        /// <param name="endpointService">Bot endpoint service.</param>
        /// <param name="telemetryClient">Bot telemetry client.</param>
        /// <param name="backgroundTaskQueue">Background task queue.</param>
        public VirtualAssistant(BotServices botServices, ConversationState conversationState, UserState userState, ProactiveState proactiveState, EndpointService endpointService, IBotTelemetryClient telemetryClient, IBackgroundTaskQueue backgroundTaskQueue)
        {
            _conversationState = conversationState ?? throw new ArgumentNullException(nameof(conversationState));
            _userState = userState ?? throw new ArgumentNullException(nameof(userState));
            _proactiveState = proactiveState ?? throw new ArgumentNullException(nameof(proactiveState));
            _services = botServices ?? throw new ArgumentNullException(nameof(botServices));
            _endpointService = endpointService ?? throw new ArgumentNullException(nameof(endpointService));
            _telemetryClient = telemetryClient ?? throw new ArgumentNullException(nameof(telemetryClient));
            _backgroundTaskQueue = backgroundTaskQueue;

            _dialogs = new DialogSet(_conversationState.CreateProperty<DialogState>(nameof(VirtualAssistant)));
            _dialogs.Add(new MainDialog(_services, _conversationState, _userState, _proactiveState, _endpointService, _telemetryClient, _backgroundTaskQueue));
        }

        /// <summary>
        /// Run every turn of the conversation. Handles orchestration of messages.
        /// </summary>
        /// <param name="turnContext">Bot Turn Context.</param>
        /// <param name="cancellationToken">Task CancellationToken.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken)
        {
            // Client notifying this bot took to long to respond (timed out)
            if (turnContext.Activity.Code == EndOfConversationCodes.BotTimedOut)
            {
                _services.TelemetryClient.TrackTrace($"Timeout in {turnContext.Activity.ChannelId} channel: Bot took too long to respond.");
                return;
            }

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