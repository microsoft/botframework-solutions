using Microsoft.Bot.Builder.Skills.Contextual.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Bot.Builder.Skills.Contextual
{
    public class UserContextResolver
    {
        private IContextResolver _contextResolver;
        private UserStateContextResolver _userStateContextResolver;

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
    }
}
