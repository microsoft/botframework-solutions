// Copyright(c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace LinkedAccounts.Web.Controllers
{
    using System;
    using System.Linq;
    using System.Net.Http;
    using System.Text;
    using System.Threading.Tasks;
    using LinkedAccounts.Web.Helpers;
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
        private ILinkedAccountRepository repository = new LinkedAccountRepository();

        public HomeController(ICredentialProvider credentialProvider, IConfiguration configuration)
        {
            this.CredentialProvider = credentialProvider;
            this.Configuration = configuration;
        }

        public IConfiguration Configuration { get; set; }

        private ICredentialProvider CredentialProvider { get; set; }

        public IActionResult Index(bool companionApp = false)
        {
            return this.RedirectToAction("LinkedAccounts", new { companionApp = companionApp });
        }

        /// <summary>
        /// Initialisation work for the Linked Accounts feature.
        /// </summary>
        /// <param name="companionApp">Flag used to show a sample deep link to a companion application.</param>
        /// <returns>IActionResult.</returns>
        public async Task<IActionResult> LinkedAccounts(bool companionApp = false)
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
            string trustedOrigin = $"{this.HttpContext.Request.Scheme}://{this.HttpContext.Request.Host}";

            request.Content = new StringContent(
                JsonConvert.SerializeObject(new { TrustedOrigins = new string[] { trustedOrigin } }),
                    Encoding.UTF8,
                    "application/json");

            var response = await client.SendAsync(request);
            string token = string.Empty;

            if (response.IsSuccessStatusCode)
            {
                // We have a Directline Token
                var body = await response.Content.ReadAsStringAsync();
                token = JsonConvert.DeserializeObject<DirectLineToken>(body).token;

                var userId = UserId.GetUserId(this.HttpContext, this.User);

                // Retrieve the status
                TokenStatus[] tokenStatuses = await this.repository.GetTokenStatusAsync(userId, this.CredentialProvider);

                // Pass the DirectLine Token, Endpont and Token Status to the View model
                return this.View(new LinkedAccountsViewModel()
                {
                    UserId = userId,
                    DirectLineToken = token,
                    Endpoint = endpoint,
                    Status = tokenStatuses,
                    CompanionApp = companionApp,
                });
            }
            else
            {
                throw new InvalidOperationException($"Exchanging a DirectLine Secret for a Token failed, check your configuration settings. Error: {response.ReasonPhrase}");
            }
        }

        /// <summary>
        /// Retrieve a URL for the user to link a given connection name to their Bot.
        /// </summary>
        /// <param name="account">TokenStatus information.</param>
        /// <returns>IActionResult.</returns>
        public async Task<IActionResult> SignIn(TokenStatus account)
        {
            var userId = UserId.GetUserId(this.HttpContext, this.User);

            string link = await this.repository.GetSignInLinkAsync(userId, this.CredentialProvider, account.ConnectionName, $"{this.Request.Scheme}://{this.Request.Host.Value}/Home/LinkedAccounts");

            return this.Redirect(link);
        }

        /// <summary>
        /// Sign a user out of a given connection name previously linked to their Bot.
        /// </summary>
        /// <param name="account">TokenStatus information.</param>
        /// <returns>IActionResult.</returns>
        public async Task<IActionResult> SignOut(TokenStatus account)
        {
            var userId = UserId.GetUserId(this.HttpContext, this.User);

            await this.repository.SignOutAsync(userId, this.CredentialProvider, account.ConnectionName);

            return this.RedirectToAction("LinkedAccounts");
        }

        public async Task<IActionResult> SignOutAll()
        {
            var userId = UserId.GetUserId(this.HttpContext, this.User);

            await this.repository.SignOutAsync(userId, this.CredentialProvider);

            return this.RedirectToAction("LinkedAccounts");
        }

        [HttpPost]
#pragma warning disable CS1998 // This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
        public async Task<IActionResult> ChangeUserId(LinkedAccountsViewModel model)
#pragma warning restore CS1998 // This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
        {
            if (this.ModelState.IsValid)
            {
                this.HttpContext.Session.SetString("ChangedUserId", model.UserId);
            }

            return this.RedirectToAction("LinkedAccounts");
        }

        public IActionResult About()
        {
            this.ViewData["Message"] = "Your application description page.";

            return this.View();
        }

        public IActionResult Contact()
        {
            this.ViewData["Message"] = "Your contact page.";

            return this.View();
        }

        [AllowAnonymous]
        public IActionResult Error()
        {
            return this.View(new ErrorViewModel { RequestId = System.Diagnostics.Activity.Current?.Id ?? this.HttpContext.TraceIdentifier });
        }
    }
}
