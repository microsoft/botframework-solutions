// Copyright(c) Microsoft Corporation.All rights reserved.
// Licensed under the MIT License.

namespace LinkedAccounts.Web.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using LinkedAccounts.Web.Helpers;
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
            var userId = UserId.GetUserId(this.HttpContext, this.User);
            Sessions[userId] = id;
        }
    }
}