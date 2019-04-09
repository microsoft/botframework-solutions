using Microsoft.Bot.Schema;

namespace Microsoft.Bot.Builder.Solutions.Shared
{
    public class ProviderTokenResponse
    {
        public OAuthProvider AuthenticationProvider { get; set; }

        public TokenResponse TokenResponse { get; set; }
    }
}