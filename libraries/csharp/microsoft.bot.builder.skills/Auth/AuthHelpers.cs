using System;
using System.Linq;
using System.Security.Claims;

namespace Microsoft.Bot.Builder.Skills.Auth
{
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