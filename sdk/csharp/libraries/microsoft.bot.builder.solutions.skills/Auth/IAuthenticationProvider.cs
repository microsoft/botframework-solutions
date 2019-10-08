using System.Security.Claims;

namespace Microsoft.Bot.Builder.Skills.Auth
{
    public interface IAuthenticationProvider
    {
        ClaimsIdentity Authenticate(string authHeader);
    }
}