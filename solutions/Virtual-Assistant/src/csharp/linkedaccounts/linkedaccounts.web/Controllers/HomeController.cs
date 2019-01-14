// Copyright(c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace LinkedAccounts.Web.Controllers
{
    using System;
    using System.Linq;
    using System.Net.Http;
    using System.Text;
    using System.Threading.Tasks;
    using LinkedAccounts.Web.Models;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Bot.Connector.Authentication;
    using Microsoft.Bot.Schema;
    using Microsoft.Extensions.Configuration;
    using Newtonsoft.Json;

    [Authorize]
    public class HomeController : Controller
    {
        public HomeController(ICredentialProvider credentialProvider, IConfiguration configuration)
        {
            this.CredentialProvider = credentialProvider;
            this.Configuration = configuration;
        }

        public IActionResult Index()
        {
            return this.View();
        }

        /// <summary>
        /// Initialisation work for the LinkedAccounts feature
        /// </summary>
        /// <returns></returns>
        public async Task<IActionResult> LinkedAccounts()
        {
            this.ViewData["Message"] = "Your application description page.";

            var secret = this.Configuration.GetSection("DirectLineSecret")?.Value;
            var endpoint = this.Configuration.GetSection("DirectLineEndpoint")?.Value;

            // First step is to exchange the DirectLine Secret for a Token
            HttpClient client = new HttpClient();
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, $"{endpoint}/tokens/generate");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", secret);
           
            // In order to avoid magic code prompts we need to set a TrustedOrigin, therefore requests using the token can be validated
            // as coming from this web-site and protecting against scenarios where a URL is shared with someone else
            string trustedOrigin = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}";

            request.Content = new StringContent(JsonConvert.SerializeObject(new { TrustedOrigins = new string[] { trustedOrigin } }),
                    Encoding.UTF8,
                    "application/json");

            var response = await client.SendAsync(request);
            string token = String.Empty;

            if (response.IsSuccessStatusCode)
            {
                // We have a Directline Token

                var body = await response.Content.ReadAsStringAsync();
                token = JsonConvert.DeserializeObject<DirectLineToken>(body).token;

                var userId = this.GetUserId();

                // Retrieve the status
                TokenStatus[] tokenStatuses = await repository.GetTokenStatusAsync(userId, CredentialProvider);

                // Pass the DirectLine Token, Endpont and Token Status to the View model
                return View(new LinkedAccountsViewModel()
                {
                    UserId = userId,
                    DirectLineToken = token,
                    Endpoint = endpoint,
                    Status = tokenStatuses
                });
            }
            else
            {
                throw new InvalidOperationException($"Exchanging a DirectLine Secret for a Token failed, check your configuration settings. Error: {response.ReasonPhrase}");
            }
        }

        /// <summary>
        /// Retrieve a URL for the user to link a given connection name to their Bot
        /// </summary>
        /// <param name="account"></param>
        /// <returns></returns>
        public async Task<IActionResult> SignIn(TokenStatus account)
        {
            var userId = GetUserId();

            string link = await repository.GetSignInLinkAsync(userId, CredentialProvider, account.ConnectionName, $"{this.Request.Scheme}://{this.Request.Host.Value}/Home/LinkedAccounts");

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

            await this.repository.SignOutAsync(userId, CredentialProvider, account.ConnectionName);

            return RedirectToAction("LinkedAccounts");
        }

        public async Task<IActionResult> SignOutAll()
        {
            var userId = GetUserId();

            await this.repository.SignOutAsync(userId, CredentialProvider);

            return RedirectToAction("LinkedAccounts");
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

        [AllowAnonymous]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = System.Diagnostics.Activity.Current?.Id ?? HttpContext.TraceIdentifier });
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

        public const string AadObjectidentifierClaim = "http://schemas.microsoft.com/identity/claims/objectidentifier";
        private ICredentialProvider CredentialProvider { get; set; }
        private ILinkedAccountRepository repository = new LinkedAccountRepository();
        public IConfiguration Configuration { get; set; }
    }
}
