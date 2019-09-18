using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Solutions.Contextual.Models;
using Microsoft.Bot.Builder.Solutions.Contextual.Rules;

namespace Microsoft.Bot.Builder.Solutions.Contextual
{
    public class UserContextManager
    {
        private IContextResolver _contextResolver;
        private UserStateContextResolver _userStateContextResolver;

        public UserContextManager(UserInfoState userInfo, IContextResolver contextResolver = null)
        {
            _contextResolver = contextResolver;
            _userStateContextResolver = new UserStateContextResolver(userInfo);
            _userStateContextResolver.RegisterARRule(new StanfordNLPRule());
            _userStateContextResolver.RegisterARRule(new CacheRule());
        }

        public static int DialogIndex { get; set; } = 0;

        internal List<PreviousTriggerIntent> PreviousTriggerIntents { get; set; } = new List<PreviousTriggerIntent>();

        internal AnaphoraResolutionState AnaphoraResolutionState { get; set; } = new AnaphoraResolutionState();

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

            // just for testing rules...
            AnaphoraResolutionState.Pron = relatedEntityInfo.PronounType;
            string name = await _userStateContextResolver.ExcuteARRules(AnaphoraResolutionState);

            // 2. User state context resolver
            var resolvedUserStateContact = await _userStateContextResolver.GetResolvedContactAsync(relatedEntityInfo);
            return resolvedUserStateContact;
        }

        public void SetDialogIndex()
        {
            DialogIndex++;
        }

        public List<string> GetPreviousTriggerIntents()
        {
            return _userStateContextResolver.GetPreviousTriggerIntents(PreviousTriggerIntents);
        }
    }
}
