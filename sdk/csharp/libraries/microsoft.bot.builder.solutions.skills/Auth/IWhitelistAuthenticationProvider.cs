using System.Collections.Generic;

namespace Microsoft.Bot.Builder.Skills.Auth
{
    public interface IWhitelistAuthenticationProvider
    {
        HashSet<string> AppsWhitelist { get; }
    }
}