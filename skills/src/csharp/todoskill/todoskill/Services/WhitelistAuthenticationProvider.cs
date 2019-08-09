using System.Collections.Generic;
using Microsoft.Bot.Builder.Skills.Auth;

namespace ToDoSkill.Services
{
    public class WhitelistAuthenticationProvider : IWhitelistAuthenticationProvider
    {
        public List<string> AppsWhitelist => new List<string>();
    }
}
