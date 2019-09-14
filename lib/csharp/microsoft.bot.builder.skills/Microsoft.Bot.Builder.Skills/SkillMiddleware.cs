using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Solutions.Skills.Models;
using Microsoft.Bot.Schema;

namespace Microsoft.Bot.Builder.Solutions.Skills
{
    /// <summary>
    /// The Skill middleware is responsible for processing Skill mode specifics, for example the skillBegin event used to signal the start of a skill conversation.
    /// </summary>
    public class SkillMiddleware : IMiddleware
    {
        private readonly ConversationState _conversationState;
        private readonly IStatePropertyAccessor<DialogState> _dialogState;

        public SkillMiddleware(ConversationState conversationState, IStatePropertyAccessor<DialogState> dialogState)
        {
            _conversationState = conversationState;
            _dialogState = dialogState;
        }

        public async Task OnTurnAsync(ITurnContext turnContext, NextDelegate next, CancellationToken cancellationToken = default)
        {
            var activity = turnContext.Activity;
            if (activity != null && activity.Type == ActivityTypes.Event)
            {
                if (activity.Name == SkillEvents.CancelAllSkillDialogsEventName)
                {
                    // when skill receives a CancelAllSkillDialogsEvent, clear the dialog stack and short-circuit
                    var currentConversation = await _dialogState.GetAsync(turnContext, () => new DialogState(), cancellationToken).ConfigureAwait(false);
					if (currentConversation.DialogStack != null)
					{
						currentConversation.DialogStack.Clear();
						await _dialogState.SetAsync(turnContext, currentConversation, cancellationToken).ConfigureAwait(false);
						await _conversationState.SaveChangesAsync(turnContext, true, cancellationToken).ConfigureAwait(false);
					}

                    return;
                }
            }

            await next(cancellationToken).ConfigureAwait(false);
        }
    }
}
