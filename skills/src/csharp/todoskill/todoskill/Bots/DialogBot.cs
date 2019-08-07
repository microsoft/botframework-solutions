// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Declarative.Resources;
using Microsoft.Bot.Builder.LanguageGeneration;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.DependencyInjection;

namespace ToDoSkill.Bots
{
    public class DialogBot<T> : IBot
        where T : Dialog
    {
        private readonly Dialog _dialog;
        private readonly BotState _conversationState;
        private readonly BotState _userState;
        private readonly IBotTelemetryClient _telemetryClient;
        private ResourceExplorer _resourceExplorer;

        public DialogBot(IServiceProvider serviceProvider, T dialog, ResourceExplorer resourceExplorer)
        {
            _dialog = dialog;
            _conversationState = serviceProvider.GetService<ConversationState>();
            _userState = serviceProvider.GetService<UserState>();
            _telemetryClient = serviceProvider.GetService<IBotTelemetryClient>();
            _resourceExplorer = resourceExplorer;
        }

        public async Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken = default(CancellationToken))
        {
            // Client notifying this bot took to long to respond (timed out)
            if (turnContext.Activity.Code == EndOfConversationCodes.BotTimedOut)
            {
                _telemetryClient.TrackTrace($"Timeout in {turnContext.Activity.ChannelId} channel: Bot took too long to respond.", Severity.Information, null);
                return;
            }

            if (turnContext.TurnState.Get<LanguageGeneratorManager>() == null)
            {
                turnContext.TurnState.Add<LanguageGeneratorManager>(new LanguageGeneratorManager(_resourceExplorer));
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


            // Save any state changes that might have occured during the turn.
            await _conversationState.SaveChangesAsync(turnContext, false, cancellationToken);
            await _userState.SaveChangesAsync(turnContext, false, cancellationToken);
        }
    }
}