// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
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
        public HomeController(ICredentialProvider provider, IConfiguration configuration)
        {
            // Retrieve the Bot configuration
            directLineSecret = configuration.GetSection("DirectLineSecret").Value;
            directLineEndpoint = configuration.GetSection("DirectLineEndpoint").Value;
            speechKey = configuration.GetSection("SpeechKey").Value;
            voiceName = configuration.GetSection("VoiceName").Value;
            credentialProvider = provider;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult WebChat()
        {
            // Get DirectLine Token
            // Pass the DirectLine Token, Speech Key and Voice Name
            // Note this approach will require magic code validation
            var directLineToken = string.Empty;
            HttpResponseMessage response = GetDirectLineTokenResponse();

            if (response.IsSuccessStatusCode)
            {
                var responseString = response.Content.ReadAsStringAsync().Result;
                var directLineResponse = JsonConvert.DeserializeObject<DirectLineResponse>(responseString);
                directLineToken = directLineResponse.Token;

                // Update as appropriate for your scenario to the unique identifier claim
                return View(new WebChatViewModel()
                {
                    DirectLineToken = directLineToken,
                    SpeechKey = speechKey,
                    VoiceName = voiceName,
                    UserID = this.GetUserId(),
                    UserName = this.GetUserName()
                });
            }
            else
            {
                throw new InvalidOperationException($"Exchanging a DirectLine Secret for a Token failed, check your configuration settings. Error: {response.ReasonPhrase}");
            }
        }

        public async Task<IActionResult> LinkedAccounts()
        {
            var directLineToken = string.Empty;
            HttpResponseMessage response = GetDirectLineTokenResponse();

            if (response.IsSuccessStatusCode)
            {
                var responseString = response.Content.ReadAsStringAsync().Result;
                var directLineResponse = JsonConvert.DeserializeObject<DirectLineResponse>(responseString);
                directLineToken = directLineResponse.Token;

                // Retrieve the object identifier for the user which will be the userID (fromID) passed to the Bot
                var userId = this.GetUserId();

                // Retrieve the status
                TokenStatus[] tokenStatuses = await repository.GetTokenStatusAsync(userId, credentialProvider);

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

        [HttpPost]
        public async Task<IActionResult> ChangeUserId(LinkedAccountsViewModel model)
        {
            if (ModelState.IsValid)
            {
                this.HttpContext.Session.SetString("ChangedUserId", model.UserId);
            }

            return RedirectToAction("LinkedAccounts");
        }

        private string GetUserId()
        {
            // If the user has overriden (to work around emulator blocker)
            if (!string.IsNullOrEmpty(HttpContext.Session.GetString("ChangedUserId")))
            {
                return HttpContext.Session.GetString("ChangedUserId");
            }
            else
            {
                var claimsIdentity = this.User?.Identity as System.Security.Claims.ClaimsIdentity;

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

        private string GetUserName()
        {
            // Retrieve the object idenitifer for the user which will be the userID (fromID) passed to the Bot
            var claimsIdentity = this.User?.Identity as ClaimsIdentity;

            if (claimsIdentity == null)
            {
                throw new InvalidOperationException("User is not logged in and needs to be.");
            }

            var objectName = claimsIdentity.Claims?.SingleOrDefault(c => c.Type == "name")?.Value;

            if (objectName == null)
            {
                throw new InvalidOperationException("User does not have a valid AAD ObjectId claim.");
            }

            return objectName;
        }

        private HttpResponseMessage GetDirectLineTokenResponse()
        {
            var directLineClient = new HttpClient();
            directLineClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", directLineSecret);

            // In order to avoid magic code prompts we need to set a TrustedOrigin, therefore requests using the token can be validated
            // as coming from this web-site and protecting against scenarios where a URL is shared with someone else
            string trustedOrigin = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}";

            var response = directLineClient.PostAsync($"{directLineEndpoint}/tokens/generate", new StringContent(JsonConvert.SerializeObject(new { TrustedOrigins = new string[] { trustedOrigin } }), Encoding.UTF8, "application/json")).Result;
            return response;
        }

        /// <summary>
        /// Retrieve a URL for the user to link a given connection name to their Bot
        /// </summary>
        /// <param name="account"></param>
        /// <returns></returns>
        public async Task<IActionResult> SignIn(TokenStatus account)
        {
            var userId = GetUserId();

            string link = await repository.GetSignInLinkAsync(userId, credentialProvider, account.ConnectionName, $"{this.Request.Scheme}://{this.Request.Host.Value}/Home/LinkedAccounts");

            return Redirect(link);
        }

        /// <summary>
        /// Sign a user out of a given connection name previously linked to their Bot
        /// </summary>
        /// <param name="account"></param>
        /// <returns></returns>
        public async Task<IActionResult> SignOut(TokenStatus account)
        {
            var userId = GetUserId();

            await this.repository.SignOutAsync(userId, credentialProvider, account.ConnectionName);

            return RedirectToAction("LinkedAccounts");
        }

        public async Task<IActionResult> SignOutAll()
        {
            var userId = GetUserId();

            await this.repository.SignOutAsync(userId, credentialProvider);

            return RedirectToAction("LinkedAccounts");
        }

        private string directLineSecret;
        private string directLineEndpoint;
        private string speechKey;
        private string voiceName;
        private ICredentialProvider credentialProvider;
        private ILinkedAccountRepository repository = new LinkedAccountRepository();
    }
}
