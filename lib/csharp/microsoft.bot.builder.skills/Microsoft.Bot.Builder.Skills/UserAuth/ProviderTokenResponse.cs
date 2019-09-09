using Microsoft.Bot.Schema;

namespace Microsoft.Bot.Builder.Skills.UserAuth
{
    public class ProviderTokenResponse
    {
        public OAuthProvider AuthenticationProvider { get; set; }

        public TokenResponse TokenResponse { get; set; }
    }
}