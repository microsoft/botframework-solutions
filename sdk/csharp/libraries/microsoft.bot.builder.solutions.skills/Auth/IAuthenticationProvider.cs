// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Security.Claims;

namespace Microsoft.Bot.Builder.Skills.Auth
{
    public interface IAuthenticationProvider
    {
        ClaimsIdentity Authenticate(string authHeader);
    }
}