// Copyright(c) Microsoft Corporation.All rights reserved.
// Licensed under the MIT License.

namespace Assistant_WebTest.Controllers
{
    using System.Threading.Tasks;
    using Microsoft.Bot.Connector.Authentication;
    using Microsoft.Bot.Schema;

    public interface ILinkedAccountRepository
    {
        Task<string> GetSignInLinkAsync(string userId, ICredentialProvider credentialProvider, string connectionName, string finalRedirect);
        Task<TokenStatus[]> GetTokenStatusAsync(string userId, ICredentialProvider credentialProvider);
        Task SignOutAsync(string userId, ICredentialProvider credentialProvider, string connectionName = null);
    }
}