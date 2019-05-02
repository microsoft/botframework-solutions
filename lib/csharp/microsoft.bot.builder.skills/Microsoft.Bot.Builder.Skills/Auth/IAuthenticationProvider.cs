namespace Microsoft.Bot.Builder.Skills.Auth
{
    public interface IAuthenticationProvider
    {
        bool Authenticate(string authHeader);
    }
}