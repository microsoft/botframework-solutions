// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Security.Claims;

namespace Microsoft.Bot.Builder.Solutions.Skills.Auth
{
    [Obsolete("This type is being deprecated. To continue using Skill capability please refer to https://aka.ms/botframework-solutions/releases/0_8", false)]
    public static class AuthHelpers
    {
        public static string GetAppIdClaimName(ClaimsIdentity claimsIdentity)
        {
            if (claimsIdentity == null)
            {
                throw new ArgumentNullException(nameof(claimsIdentity));
            }

            // version "1.0" tokens include the "appid" claim and version "2.0" tokens include the "azp" claim
            var appIdClaimName = "appid";
            var tokenVersion = claimsIdentity.Claims.SingleOrDefault(c => c.Type == "ver")?.Value;
            if (tokenVersion == "2.0")
            {
                appIdClaimName = "azp";
            }

            return appIdClaimName;
        }
    }
}