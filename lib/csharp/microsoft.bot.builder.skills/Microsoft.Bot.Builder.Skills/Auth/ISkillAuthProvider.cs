using Microsoft.AspNetCore.Http;

namespace Microsoft.Bot.Builder.Skills.Auth
{
    public interface ISkillAuthProvider
    {
        bool Authenticate(HttpContext httpContext);
    }
}