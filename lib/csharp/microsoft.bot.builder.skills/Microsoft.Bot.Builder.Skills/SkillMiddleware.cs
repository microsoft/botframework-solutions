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
            // The skillBegin event signals the start of a skill conversation to a Bot.
            var activity = turnContext.Activity;
            if (activity != null && activity.Type == ActivityTypes.Event)
            {
                if (activity.Name == SkillEvents.SkillBeginEventName && activity.Value != null)
                {
                    var skillContext = activity.Value as SkillContext;
                    if (skillContext != null)
                    {
                        var accessor = _userState.CreateProperty<SkillContext>(nameof(SkillContext));
                        await accessor.SetAsync(turnContext, skillContext);
                    }
                }
                else if (activity.Name == SkillEvents.CancelAllSkillDialogsEventName)
                {
                    // when skill receives a CancelAllSkillDialogsEvent, clear the dialog stack and short-circuit
                    var currentConversation = await _dialogState.GetAsync(turnContext);
                    currentConversation.DialogStack.Clear();
                    await _dialogState.SetAsync(turnContext, currentConversation);
                    await _conversationState.SaveChangesAsync(turnContext, true);

                    return;
                }
            }

            await next(cancellationToken).ConfigureAwait(false);
        }
    }
}