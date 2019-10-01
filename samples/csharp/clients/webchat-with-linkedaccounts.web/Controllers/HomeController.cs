// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Assistant_WebTest.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Activity = System.Diagnostics.Activity;

namespace Assistant_WebTest.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        public const string AadObjectidentifierClaim = "http://schemas.microsoft.com/identity/claims/objectidentifier";
        private readonly ICredentialProvider _credentialProvider;
        private readonly string _directLineEndpoint;

        private readonly string _directLineSecret;
        private readonly ILinkedAccountRepository _repository = new LinkedAccountRepository();
        private readonly string _speechKey;
        private readonly string _voiceName;
        private readonly string _speechRegion;

        public HomeController(ICredentialProvider provider, IConfiguration configuration)
        {
            // Retrieve the Bot configuration
            _directLineSecret = configuration.GetSection("DirectLineSecret").Value;
            _directLineEndpoint = configuration.GetSection("DirectLineEndpoint").Value;
            _speechKey = configuration.GetSection("SpeechKey").Value;
            _speechRegion = configuration.GetSection("SpeechRegion").Value;
            _voiceName = configuration.GetSection("VoiceName").Value;
            _credentialProvider = provider;
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
            var response = GetDirectLineTokenResponse();

            if (response.IsSuccessStatusCode)
            {
                var responseString = response.Content.ReadAsStringAsync().Result;
                var directLineResponse = JsonConvert.DeserializeObject<DirectLineResponse>(responseString);
                var directLineToken = directLineResponse.Token;

                // Update as appropriate for your scenario to the unique identifier claim
                return View(new WebChatViewModel
                {
                    DirectLineToken = directLineToken,
                    SpeechKey = _speechKey,
                    SpeechRegion = _speechRegion,
                    VoiceName = _voiceName,
                    UserID = GetUserId(),
                    UserName = GetUserName()
                });
            }

            throw new InvalidOperationException($"Exchanging a DirectLine Secret for a Token failed, check your configuration settings. Error: {response.ReasonPhrase}");
        }

        public async Task<IActionResult> LinkedAccounts()
        {
            var response = GetDirectLineTokenResponse();

            if (response.IsSuccessStatusCode)
            {
                var responseString = response.Content.ReadAsStringAsync().Result;
                var directLineResponse = JsonConvert.DeserializeObject<DirectLineResponse>(responseString);
                var directLineToken = directLineResponse.Token;

                // Retrieve the object identifier for the user which will be the userID (fromID) passed to the Bot
                var userId = GetUserId();

                // Retrieve the status
                var tokenStatuses = await _repository.GetTokenStatusAsync(userId, _credentialProvider);

                // Pass the User Id, Direct Line Token, Endpoint and Token Status to the View model
                return View(new LinkedAccountsViewModel
                {
                    UserId = userId,
                    DirectLineToken = directLineToken,
                    Endpoint = _directLineEndpoint,
                    Status = tokenStatuses
                });
            }

            throw new InvalidOperationException($"Exchanging a DirectLine Secret for a Token failed, check your configuration settings. Error: {response.ReasonPhrase}");
        }

        [AllowAnonymous]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [HttpPost]
        public async Task<IActionResult> ChangeUserId(LinkedAccountsViewModel model)
        {
            if (ModelState.IsValid)
            {
                HttpContext.Session.SetString("ChangedUserId", model.UserId);
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

            var claimsIdentity = User?.Identity as ClaimsIdentity;

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

        private string GetUserName()
        {
            // Retrieve the object identifier for the user which will be the userID (fromID) passed to the Bot
            var claimsIdentity = User?.Identity as ClaimsIdentity;

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
            directLineClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _directLineSecret);

            // In order to avoid magic code prompts we need to set a TrustedOrigin, therefore requests using the token can be validated
            // as coming from this web-site and protecting against scenarios where a URL is shared with someone else
            var trustedOrigin = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}";

            var response = directLineClient.PostAsync($"{_directLineEndpoint}/tokens/generate", new StringContent(JsonConvert.SerializeObject(new { TrustedOrigins = new[] { trustedOrigin } }), Encoding.UTF8, "application/json")).Result;
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

            var link = await _repository.GetSignInLinkAsync(userId, _credentialProvider, account.ConnectionName, $"{Request.Scheme}://{Request.Host.Value}/Home/LinkedAccounts");

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

            await _repository.SignOutAsync(userId, _credentialProvider, account.ConnectionName);

            return RedirectToAction("LinkedAccounts");
        }

        public async Task<IActionResult> SignOutAll()
        {
            var userId = GetUserId();

            await _repository.SignOutAsync(userId, _credentialProvider);

            return RedirectToAction("LinkedAccounts");
        }
    }
}