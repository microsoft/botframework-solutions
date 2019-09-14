using System.Security.Claims;

namespace Microsoft.Bot.Builder.Solutions.Skills.Auth
{
    public interface IAuthenticationProvider
    {
        ClaimsIdentity Authenticate(string authHeader);
    }
}
