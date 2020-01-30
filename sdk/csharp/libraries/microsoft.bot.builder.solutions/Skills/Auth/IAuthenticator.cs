// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Microsoft.Bot.Builder.Solutions.Skills.Auth
{
    [Obsolete("This type is being deprecated. To continue using Skill capability please refer to https://aka.ms/botframework-solutions/releases/0_8", false)]
    public interface IAuthenticator
    {
        Task<ClaimsIdentity> AuthenticateAsync(HttpRequest httpRequest, HttpResponse httpResponse);
    }
}