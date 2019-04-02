﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.DependencyInjection;
using VirtualAssistantTemplate.Services;

namespace VirtualAssistantTemplate.Bots
{
    public class Bot<T> : ActivityHandler where T : Dialog
    {
        private readonly IBotTelemetryClient _telemetryClient;
        private DialogSet _dialogs;

        public Bot(IServiceProvider serviceProvider, T dialog)
        {
            var services = serviceProvider.GetService<BotServices>() ?? throw new ArgumentNullException(nameof(BotServices));
            var conversationState = serviceProvider.GetService<ConversationState>() ?? throw new ArgumentNullException(nameof(ConversationState));
            var userState = serviceProvider.GetService<UserState>() ?? throw new ArgumentNullException(nameof(UserState));
            _telemetryClient = serviceProvider.GetService<IBotTelemetryClient>() ?? throw new ArgumentNullException(nameof(IBotTelemetryClient));

            var dialogState = conversationState.CreateProperty<DialogState>(nameof(VirtualAssistantTemplate));
            _dialogs = new DialogSet(dialogState);
            _dialogs.Add(dialog);
        }

        public override async Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken)
        {
            // Client notifying this bot took to long to respond (timed out)
            if (turnContext.Activity.Code == EndOfConversationCodes.BotTimedOut)
            {
                _telemetryClient.TrackTrace($"Timeout in {turnContext.Activity.ChannelId} channel: Bot took too long to respond.", Severity.Information, null);
                return;
            }

            var dc = await _dialogs.CreateContextAsync(turnContext);

            if (dc.ActiveDialog != null)
            {
                var result = await dc.ContinueDialogAsync();
            }
            else
            {
                await dc.BeginDialogAsync(typeof(T).Name);
            }
        }
    }
}
