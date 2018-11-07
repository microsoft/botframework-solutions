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
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;

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
            speechKey = configuration.GetSection("SpeechKey").Value;
            voiceName = configuration.GetSection("VoiceName").Value;
        }

        public IActionResult Index()
        {
            // Get DirectLine Token
            // Pass the DirectLine Token, Speech Key and Voice Name
            // Note this approach will require magic code validation
            var directLineToken = string.Empty;
            var directLineClient = new HttpClient();
            directLineClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", directLineSecret);

            var response = directLineClient.PostAsync("https://directline.botframework.com/v3/directline/tokens/generate", new StringContent(string.Empty, Encoding.UTF8, "application/json")).Result;

            if (response.IsSuccessStatusCode)
            {
                var responseString = response.Content.ReadAsStringAsync().Result;
                var directLineResponse = JsonConvert.DeserializeObject<DirectlineResponse>(responseString);
                directLineToken = directLineResponse.token;
            }

            ViewData["DirectLineToken"] = directLineToken;
            ViewData["SpeechKey"] = speechKey;
            ViewData["VoiceName"] = voiceName;

            // Retrieve the object idenitifer for the user which will be the userID (fromID) passed to the Bot
            var claimsIdentity = this.User?.Identity as System.Security.Claims.ClaimsIdentity;

            if (claimsIdentity == null)
            {
                throw new InvalidOperationException("User is not logged in and needs to be.");
            }

            // Update as appropriate for your scenario to the unique identifier claim
            var useriD = claimsIdentity.Claims?.SingleOrDefault(c => c.Type == HomeController.AadObjectidentifierClaim)?.Value;
            var userName = claimsIdentity.Claims?.SingleOrDefault(c => c.Type == "name").Value;

            ViewData["UserId"] = useriD;
            ViewData["UserName"] = userName;

            return View();
        }

        [AllowAnonymous]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        private string directLineSecret;
        private string speechKey;
        private string voiceName;
    }
}
