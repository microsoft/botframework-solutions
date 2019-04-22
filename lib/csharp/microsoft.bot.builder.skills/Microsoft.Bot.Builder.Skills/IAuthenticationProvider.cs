using System.Threading.Tasks;
using Microsoft.Bot.Connector.Authentication;

namespace Microsoft.Bot.Builder.Skills
{
    public interface IAuthenticationProvider
    {
        Task<bool> AuthenticateAsync(string authHeader, ICredentialProvider credentialProvider);
    }
}