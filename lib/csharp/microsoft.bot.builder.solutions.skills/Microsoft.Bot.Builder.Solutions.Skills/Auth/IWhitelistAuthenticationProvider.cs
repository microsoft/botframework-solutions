using System.Collections.Generic;

namespace Microsoft.Bot.Builder.Solutions.Skills.Auth
{
    public interface IWhitelistAuthenticationProvider
    {
        HashSet<string> AppsWhitelist { get; }
    }
}
