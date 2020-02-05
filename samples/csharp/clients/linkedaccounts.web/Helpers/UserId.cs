using System;
using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace LinkedAccounts.Web.Helpers
{
    public class UserId
    {
        public const string AadObjectidentifierClaim = "http://schemas.microsoft.com/identity/claims/objectidentifier";

        public static string GetUserId(HttpContext httpContext, ClaimsPrincipal user)
        {
            var claimsIdentity = user?.Identity as System.Security.Claims.ClaimsIdentity;

            if (claimsIdentity == null)
            {
                throw new InvalidOperationException("User is not logged in and needs to be.");
            }

            var objectId = claimsIdentity.Claims?.SingleOrDefault(c => c.Type == AadObjectidentifierClaim)?.Value;

            if (objectId == null)
            {
                throw new InvalidOperationException("User does not have a valid AAD ObjectId claim.");
            }

            return objectId;
        }
    }
}
