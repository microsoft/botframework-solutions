using Microsoft.Bot.Schema;

namespace Microsoft.Bot.Solutions.Authentication
{
    public class ProviderTokenResponse
    {
        public OAuthProvider AuthenticationProvider { get; set; }

        public TokenResponse TokenResponse { get; set; }
    }
}
