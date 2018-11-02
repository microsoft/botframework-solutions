// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using $safeprojectname$.Extensions;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;

namespace $safeprojectname$
{
    /// <summary>
    /// Main entry point and orchestration for bot.
    /// </summary>
    public class $safeprojectname$ : IBot
    {
        private readonly BotServices _services;
        private readonly ConversationState _conversationState;
        private readonly UserState _userState;
        private DialogSet _dialogs;

        /// <summary>
        /// Initializes a new instance of the <see cref="$safeprojectname$"/> class.
        /// </summary>
        /// <param name="botServices">Bot services.</param>
        /// <param name="conversationState">Bot conversation state.</param>
        /// <param name="userState">Bot user state.</param>
        public $safeprojectname$(BotServices botServices, ConversationState conversationState, UserState userState)
        {
            _conversationState = conversationState ?? throw new ArgumentNullException(nameof(conversationState));
            _userState = userState ?? throw new ArgumentNullException(nameof(userState));
            _services = botServices ?? throw new ArgumentNullException(nameof(botServices));

            _dialogs = new DialogSet(_conversationState.CreateProperty<DialogState>(nameof($safeprojectname$)));
            _dialogs.Add(new MainDialog(_services, _conversationState, _userState));
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
                // Don't respond because channel won't accept response and which may result in Exception
                return;
            }

            var dc = await _dialogs.CreateContextAsync(turnContext);
            var result = await dc.ContinueDialogAsync();

            if (result.Status == DialogTurnStatus.Empty)
            {
                // Handle Cortana's launch action
                if (CortanaHelper.IsLaunchActivity(turnContext.Activity))
                {
                    await dc.BeginDialogAsync(nameof(MainDialog));
                }
                else if (turnContext.Activity.Type == ActivityTypes.ConversationUpdate)
                {
                    var activity = turnContext.Activity.AsConversationUpdateActivity();

                    // if conversation update is not from the bot.
                    if (!activity.MembersAdded.Any(m => m.Id == activity.Recipient.Id))
                    {
                        await dc.BeginDialogAsync(nameof(MainDialog));
                    }
                }
            }
        }
    }
}
