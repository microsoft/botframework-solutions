using Microsoft.Bot.Schema;

namespace Microsoft.Bot.Builder.Solutions.Authentication
{
    public class ProviderTokenResponse
    {
        public OAuthProvider AuthenticationProvider { get; set; }

        public TokenResponse TokenResponse { get; set; }
    }
}
