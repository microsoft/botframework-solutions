// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Configuration;
using Microsoft.Bot.Schema;

namespace VirtualAssistant
{
    /// <summary>
    /// Main entry point and orchestration for bot.
    /// </summary>
    public class VirtualAssistant : IBot
    {
        private readonly BotServices _services;
        private readonly BotConfiguration _botConfig;
        private readonly ConversationState _conversationState;
        private readonly UserState _userState;
        private readonly EndpointService _endpointService;
        private DialogSet _dialogs;

        /// <summary>
        /// Initializes a new instance of the <see cref="VirtualAssistant"/> class.
        /// </summary>
        /// <param name="botConfig">Bot configuration.</param>
        /// <param name="botServices">Bot services.</param>
        /// <param name="conversationState">Bot conversation state.</param>
        /// <param name="userState">Bot user state.</param>
        /// <param name="endpointService">Bot endpoint service.</param>
        public VirtualAssistant(BotServices botServices, BotConfiguration botConfig, ConversationState conversationState, UserState userState, EndpointService endpointService)
        {
            _conversationState = conversationState ?? throw new ArgumentNullException(nameof(conversationState));
            _userState = userState ?? throw new ArgumentNullException(nameof(userState));
            _services = botServices ?? throw new ArgumentNullException(nameof(botServices));
            _endpointService = endpointService ?? throw new ArgumentNullException(nameof(endpointService));
            _botConfig = botConfig;

            _dialogs = new DialogSet(_conversationState.CreateProperty<DialogState>(nameof(VirtualAssistant)));
            _dialogs.Add(new MainDialog(_services, _botConfig, _conversationState, _userState, _endpointService));
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
        }
    }
}