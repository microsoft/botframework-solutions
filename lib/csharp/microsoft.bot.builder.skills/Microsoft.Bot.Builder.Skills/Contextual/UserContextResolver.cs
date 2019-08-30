using Microsoft.Bot.Builder.Skills.Contextual.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Bot.Builder.Skills.Contextual
{
    public class UserContextResolver
    {
        private IUserContextResolver _contextResolver;

        public UserContextResolver(UserInfoState userInfo, IUserContextResolver contextResolver = null)
        {
            this._contextResolver = contextResolver;
        }

        //public string GetResolvedContext(string context)
        //{

        //}

    }
}
