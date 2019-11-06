// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Skills.Models;
using Microsoft.Bot.Schema;

namespace Microsoft.Bot.Builder.Skills
{
    /// <summary>
    /// The Skill middleware is responsible for processing Skill mode specifics, for example the skillBegin event used to signal the start of a skill conversation.
    /// </summary>
    public class SkillMiddleware : IMiddleware
    {
        private UserState _userState;
        private ConversationState _conversationState;
        private IStatePropertyAccessor<DialogState> _dialogState;

        public SkillMiddleware(UserState userState, ConversationState conversationState, IStatePropertyAccessor<DialogState> dialogState)
        {
            _userState = userState;
            _conversationState = conversationState;
            _dialogState = dialogState;
        }

        public async Task OnTurnAsync(ITurnContext turnContext, NextDelegate next, CancellationToken cancellationToken = default(CancellationToken))
        {
            var activity = turnContext.Activity;
            if (activity != null && activity.Type == ActivityTypes.Event)
            {
                if (activity.Name == SkillEvents.CancelAllSkillDialogsEventName)
                {
                    await _dialogState.DeleteAsync(turnContext).ConfigureAwait(false);
                    await _conversationState.ClearStateAsync(turnContext).ConfigureAwait(false);
                    await _conversationState.SaveChangesAsync(turnContext, force: true).ConfigureAwait(false);
                    return;
                }
            }

            await next(cancellationToken).ConfigureAwait(false);
        }
    }
}