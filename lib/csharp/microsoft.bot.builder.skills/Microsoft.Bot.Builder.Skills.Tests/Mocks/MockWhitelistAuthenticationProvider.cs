using System.Collections.Generic;
using Microsoft.Bot.Builder.Solutions.Skills.Auth;

namespace Microsoft.Bot.Builder.Solutions.Skills.Tests.Mocks
{
    public class MockWhitelistAuthenticationProvider : IWhitelistAuthenticationProvider
    {
        public HashSet<string> AppsWhitelist => new HashSet<string>();
    }
}
