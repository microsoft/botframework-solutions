﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Solutions.Contextual.Models;

namespace Microsoft.Bot.Builder.Solutions.Contextual
{
    public class UserContextManager
    {
        private IContextResolver _contextResolver;
        private UserStateContextResolver _userStateContextResolver;

        public UserContextManager(UserInfoState userInfo, IContextResolver contextResolver = null)
        {
            _contextResolver = contextResolver;
            _userStateContextResolver = new UserStateContextResolver(userInfo, PreviousContacts);
        }

        public static int DialogIndex { get; set; } = 0;

        internal List<PreviousTriggerIntent> PreviousTriggerIntents { get; set; } = new List<PreviousTriggerIntent>();

        internal List<string> PreviousContacts { get; set; } = new List<string>();

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

        public List<PreviousTriggerIntent> GetPreviousQuestions()
        {
            return PreviousTriggerIntents;
        }

        public async Task ClearPreviousQuestions(ITurnContext turnContext)
        {
            PreviousTriggerIntents.Clear();
        }
    }
}
