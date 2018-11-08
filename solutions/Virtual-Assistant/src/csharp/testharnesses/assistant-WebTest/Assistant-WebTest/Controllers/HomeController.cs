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
using Newtonsoft.Json;using System.Security.Claims;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Connector.Authentication;

namespace Assistant_WebTest.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {        
        public const string AadObjectidentifierClaim = "http://schemas.microsoft.com/identity/claims/objectidentifier";
        public HomeController(ICredentialProvider credentialProvider, IConfiguration configuration)
        {
            // Retrieve the Bot configuration
            directLineSecret = configuration.GetSection("DirectLineSecret").Value;
            directLineEndpoint = configuration.GetSection("DirectLineEndpoint").Value;
            speechKey = configuration.GetSection("SpeechKey").Value;
            voiceName = configuration.GetSection("VoiceName").Value;
            CredentialProvider = credentialProvider;
        }

        public IActionResult Index()
        {
            // Get DirectLine Token
            // Pass the DirectLine Token, Speech Key and Voice Name
            // Note this approach will require magic code validation
            var directLineToken = string.Empty;
            var directLineClient = new HttpClient();
            directLineClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", directLineSecret);

            var response = directLineClient.PostAsync($"{directLineEndpoint}/tokens/generate", new StringContent(string.Empty, Encoding.UTF8, "application/json")).Result;

            if (response.IsSuccessStatusCode)
            {
                var responseString = response.Content.ReadAsStringAsync().Result;
                var directLineResponse = JsonConvert.DeserializeObject<DirectLineResponse>(responseString);
                directLineToken = directLineResponse.token;

                ViewData["DirectLineToken"] = directLineToken;
                ViewData["SpeechKey"] = speechKey;
                ViewData["VoiceName"] = voiceName;

                // Retrieve the object idenitifer for the user which will be the userID (fromID) passed to the Bot
                var claimsIdentity = this.User?.Identity as ClaimsIdentity;

                if (claimsIdentity == null)
                {
                    throw new InvalidOperationException("User is not logged in and needs to be.");
                }

                // Update as appropriate for your scenario to the unique identifier claim
                var userId = claimsIdentity.Claims?.SingleOrDefault(c => c.Type == HomeController.AadObjectidentifierClaim)?.Value;
                var userName = claimsIdentity.Claims?.SingleOrDefault(c => c.Type == "name").Value;

                ViewData["UserId"] = this.GetUserId(claimsIdentity);
                ViewData["UserName"] = this.GetUserName(claimsIdentity);

                return View();
            }
            else
            {
                throw new InvalidOperationException($"Exchanging a DirectLine Secret for a Token failed, check your configuration settings. Error: {response.ReasonPhrase}");
            }
        }

        public async Task<IActionResult> LinkedAccounts()
        {
            var directLineToken = string.Empty;
            var directLineClient = new HttpClient();
            directLineClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", directLineSecret);

            var response = directLineClient.PostAsync($"{directLineEndpoint}/tokens/generate", new StringContent(string.Empty, Encoding.UTF8, "application/json")).Result;

            if (response.IsSuccessStatusCode)
            {
                var responseString = response.Content.ReadAsStringAsync().Result;
                var directLineResponse = JsonConvert.DeserializeObject<DirectLineResponse>(responseString);
                directLineToken = directLineResponse.token;

                // Retrieve the object idenitifer for the user which will be the userID (fromID) passed to the Bot
                var claimsIdentity = this.User?.Identity as ClaimsIdentity;

                if (claimsIdentity == null)
                {
                    throw new InvalidOperationException("User is not logged in and needs to be.");
                }

                var userId = this.GetUserId(claimsIdentity);

                // Retrieve the status
                TokenStatus[] tokenStatuses = await repository.GetTokenStatusAsync(userId, CredentialProvider);

                // Pass the User Id, Direct Line Token, Endpoint and Token Status to the View model
                return View(new LinkedAccountsViewModel()
                {
                    UserId = userId,
                    DirectLineToken = directLineToken,
                    Endpoint = directLineEndpoint,
                    Status = tokenStatuses
                });
            }
            else
            {
                throw new InvalidOperationException($"Exchanging a DirectLine Secret for a Token failed, check your configuration settings. Error: {response.ReasonPhrase}");
            }
        }

        [AllowAnonymous]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = System.Diagnostics.Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        private string GetUserId(ClaimsIdentity claimsIdentity)
        {
            var objectId = claimsIdentity.Claims?.SingleOrDefault(c => c.Type == AadObjectidentifierClaim)?.Value;

            if (objectId == null)
            {
                throw new InvalidOperationException("User does not have a valid AAD ObjectId claim.");
            }

            return objectId;
        }

        private string GetUserName(ClaimsIdentity claimsIdentity)
        {

            var objectId = claimsIdentity.Claims?.SingleOrDefault(c => c.Type == "name")?.Value;

            if (objectId == null)
            {
                throw new InvalidOperationException("User does not have a valid AAD ObjectId claim.");
            }

            return objectId;
        }

        private string directLineSecret;
        private string directLineEndpoint;
        private string speechKey;
        private string voiceName;
        private ICredentialProvider CredentialProvider;
        private ILinkedAccountRepository repository = new LinkedAccountRepository();
    }
}
