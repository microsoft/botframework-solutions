// Copyright(c) Microsoft Corporation.All rights reserved.
// Licensed under the MIT License.



namespace LinkedAccounts.Web.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;

    [Authorize]
    [Produces("application/json")]
    [Route("api/Session")]
    public class SessionController : Controller
    {
        public static Dictionary<string, string> Sessions = new Dictionary<string, string>();
        
        // POST api/values
        [HttpPost]
        public void Post([FromQuery]string id)
        {
            // Passed the SessionID from the View which we store against the UserId for later use.
            var userId = GetUserId();
            Sessions[userId] = id;
        }

        private string GetUserId()
        {
            var claimsIdentity = this.User?.Identity as System.Security.Claims.ClaimsIdentity;

            if (claimsIdentity == null)
            {
                throw new InvalidOperationException("User is not logged in and needs to be.");
            }

            // Update as appropriate for your scenario to the unique identifier claim
            var objectId = claimsIdentity.Claims?.SingleOrDefault(c => c.Type == HomeController.AadObjectidentifierClaim)?.Value;

            if (objectId == null)
            {
                throw new InvalidOperationException("User does not have a valid AAD ObjectId claim.");
            }

            return objectId;
        }
    }
}