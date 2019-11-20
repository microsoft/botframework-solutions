// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Microsoft.Bot.Builder.Solutions.Skills.Auth
{
    public interface IAuthenticator
    {
        Task<ClaimsIdentity> AuthenticateAsync(HttpRequest httpRequest, HttpResponse httpResponse);
    }
}