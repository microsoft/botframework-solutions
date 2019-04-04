using Microsoft.AspNetCore.Http;
using System;
using System.Linq;
using System.Security.Claims;

namespace Microsoft.Bot.Builder.Skills.Auth
{
    public class JwtClaimAuthProvider : ISkillAuthProvider
    {
        private readonly ISkillWhitelist _skillWhitelist;

        public JwtClaimAuthProvider(ISkillWhitelist skillWhitelist)
        {
            _skillWhitelist = skillWhitelist;
        }

        public bool Authenticate(HttpContext httpContext)
        {
            if (httpContext == null)
            {
                throw new ArgumentNullException(nameof(HttpContext));
            }

            // if not provided a list, ignore this authentication operation
            if (_skillWhitelist == null || _skillWhitelist.SkillWhiteList == null || _skillWhitelist.SkillWhiteList.Count() == 0)
            {
                return true;
            }

            var identity = httpContext.User.Identity;
            if (identity is ClaimsIdentity claims)
            {
                var version = claims.Claims.FirstOrDefault(c => c.Type.Equals("ver", StringComparison.InvariantCultureIgnoreCase));
                var appId = string.Empty;
                var appIdClaimType = string.Empty;
                if (version.Value.Equals("1.0"))
                {
                    appIdClaimType = "appid";
                }
                else if (version.Value.Equals("2.0"))
                {
                    appIdClaimType = "azp";
                }

                var appIdClaim = claims.Claims.FirstOrDefault(c => c.Type.Equals(appIdClaimType, StringComparison.InvariantCulture));
                if (appIdClaim != null)
                {
                    appId = appIdClaim.Value;
                }

                if (_skillWhitelist.SkillWhiteList.Contains(appId))
                {
                    return true;
                }
            }

            return false;
        }
    }
}