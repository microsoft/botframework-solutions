using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Solutions.Contextual.Models;

namespace Microsoft.Bot.Builder.Solutions.Contextual
{
    public class UserContextResolver
    {
        private IContextResolver _contextResolver;
        private UserStateContextResolver _userStateContextResolver;

        public static int DialogIndex { get; set; } = 0;

        public IUserContext UserContext { get; set; }

        public UserContextResolver(UserInfoState userInfo, IContextResolver contextResolver = null)
        {
            _contextResolver = contextResolver;
            _userStateContextResolver = new UserStateContextResolver(userInfo);
        }

        public async Task<IList<string>> GetResolvedContactAsync(RelatedEntityInfo relatedEntityInfo)
        {
            // Take result as following priority:
            // 1. Injection context resolver
            if (_contextResolver != null)
            {
                var resolvedContact = await _contextResolver.GetResolvedContactAsync(relatedEntityInfo);

                if (resolvedContact != null)
                {
                    return resolvedContact;
                }
            }

            // 2. User state context resolver
            var resolvedUserStateContact = await _userStateContextResolver.GetResolvedContactAsync(relatedEntityInfo);
            return resolvedUserStateContact;
        }

        public void SetDialogIndex()
        {
            DialogIndex++;
        }

        public async Task ShowPreviousQuestion(ITurnContext turnContext)
        {
            var questionAccessor = turnContext.TurnState.Get<IStatePropertyAccessor<List<PreviousQuestion>>>();
            var questions = await questionAccessor.GetAsync(turnContext, () => new List<PreviousQuestion>());
            var actions = questions.Select(x => x.Utterance).ToList();
            var activity = MessageFactory.SuggestedActions(actions);
            await turnContext.SendActivityAsync(activity);
        }

        public async Task ClearPreviousQuestions(ITurnContext turnContext)
        {
            var questionAccessor = turnContext.TurnState.Get<IStatePropertyAccessor<List<PreviousQuestion>>>();
            await questionAccessor.DeleteAsync(turnContext);
        }
    }
}
