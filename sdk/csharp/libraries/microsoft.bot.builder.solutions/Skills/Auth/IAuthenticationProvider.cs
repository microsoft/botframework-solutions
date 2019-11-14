// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Security.Claims;
using System.Threading.Tasks;

namespace Microsoft.Bot.Builder.Solutions.Skills.Auth
{
    public interface IAuthenticationProvider
    {
        Task<ClaimsIdentity> AuthenticateAsync(string authHeader);
    }
}