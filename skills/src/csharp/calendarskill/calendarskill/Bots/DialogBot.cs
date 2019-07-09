// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.DependencyInjection;

namespace CalendarSkill.Bots
{
    public class DialogBot<T> : ActivityHandler
        where T : Dialog
    {
        private readonly IBotTelemetryClient _telemetryClient;
        private DialogSet _dialogs;
        private DialogManager _dialogManager;
        private IStorage _storage;

        public DialogBot(IServiceProvider serviceProvider, T dialog, IStorage storage)
        {
            var conversationState = serviceProvider.GetService<ConversationState>() ?? throw new ArgumentNullException(nameof(ConversationState));
            _telemetryClient = serviceProvider.GetService<IBotTelemetryClient>() ?? throw new ArgumentNullException(nameof(IBotTelemetryClient));

            var dialogState = conversationState.CreateProperty<DialogState>(nameof(CalendarSkill));
            _dialogs = new DialogSet(dialogState);
            _dialogs.Add(dialog);

            _dialogManager = new DialogManager(dialog);
            _storage = storage;
        }

        public override Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken)
        {
            // Client notifying this bot took to long to respond (timed out)
            if (turnContext.Activity.Code == EndOfConversationCodes.BotTimedOut)
            {
                _telemetryClient.TrackTrace($"Timeout in {turnContext.Activity.ChannelId} channel: Bot took too long to respond.", Severity.Information, null);
                return Task.CompletedTask;
            }

            if (turnContext.TurnState.Get<IStorage>() == null)
            {
                turnContext.TurnState.Add<IStorage>(_storage);
            }

            return _dialogManager.OnTurnAsync(turnContext, cancellationToken: cancellationToken);
        }
    }
}