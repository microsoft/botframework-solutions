// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Assistant_WebTest.Models;
using Microsoft.Extensions.Configuration;

namespace Assistant_WebTest.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {        
        public const string AadObjectidentifierClaim = "http://schemas.microsoft.com/identity/claims/objectidentifier";
        public HomeController(IConfiguration configuration)
        {
            // Retrieve the Bot configuration
            directLineSecret = configuration.GetSection("DirectLineSecret").Value;
            botId = configuration.GetSection("BotId").Value;          
        }

        public IActionResult Index()
        {
            // Pass the DirectLine Secret and BotId to the Bot control
            // Note this approach will require magic code validation
            ViewData["DirectLineSecret"] = directLineSecret;
            ViewData["BotId"] = botId;

            // Retrieve the object idenitifer for the user which will be the userID (fromID) passed to the Bot
            var claimsIdentity = this.User?.Identity as System.Security.Claims.ClaimsIdentity;

            if (claimsIdentity == null)
            {
                throw new InvalidOperationException("User is not logged in and needs to be.");
            }

            // Update as appropriate for your scenario to the unique identifier claim
            var useriD = claimsIdentity.Claims?.SingleOrDefault(c => c.Type == HomeController.AadObjectidentifierClaim)?.Value;
            ViewData["UserId"] = useriD;           

            return View();
        }

        public IActionResult About()
        {
            ViewData["Message"] = "Your application description page.";

            return View();
        }

        public IActionResult Contact()
        {
            ViewData["Message"] = "Your contact page.";

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [AllowAnonymous]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        private string directLineSecret;
        private string botId;
    }
}
