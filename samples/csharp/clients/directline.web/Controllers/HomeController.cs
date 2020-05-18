// Copyright(c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using DirectLine.Web.Configuration;
using DirectLine.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace DirectLine.Web.Controllers
{
    public class HomeController : Controller
    {
        private const string DirectLineMode = "DIRECTLINE";
        private const string DirectLineSpeechMode = "DIRECTLINESPEECH";
        private const string GenerateDirectLineTokenUrl = "https://directline.botframework.com/v3/directline/tokens/generate";
        private const string UnknownMode = "UNKNOWN";

        private readonly DirectLineWebConfiguration configuration;
        private readonly string mode;
        private readonly string userId;

        public HomeController(IConfiguration configuration)
        {
            // Retrieve the Direct Line Web application configuration
            this.configuration = configuration.Get<DirectLineWebConfiguration>();

            if (this.configuration.EnableDirectLineEnhancedAuthentication)
            {
                this.userId = $"dl_{Guid.NewGuid()}";
            }

            // The method in which authentication token retrieval and renewal is handled, as well as configuration
            // for the web chat component, is dependent upon whether Direct Line or Direct Line Speech is being
            // utilized.
            //
            // Determine which mode to operate under:
            // - Direct Line Speech should be used if a speech service region identifier and key are provided
            // - Direct Line should be used if a DL secret is provided
            // - Default to an unknown state (i.e. invalid app configuration provided)
            if (!string.IsNullOrEmpty(this.configuration?.SpeechServiceRegionIdentifier) &&
                !string.IsNullOrEmpty(this.configuration?.SpeechServiceSubscriptionKey))
            {
                this.mode = DirectLineSpeechMode;
            }
            else if (!string.IsNullOrEmpty(this.configuration?.DirectLineSecret))
            {
                this.mode = DirectLineMode;
            }
            else
            {
                this.mode = UnknownMode;
            }
        }

        public IActionResult Index(string locale = "en-us")
        {
            // Passing the User ID through to the web chat component for Enhanced Authentication
            // is only supported in Direct Line mode.
            bool isDirectLineMode = string.Equals(DirectLineMode, this.mode, StringComparison.OrdinalIgnoreCase);

            ViewData["Locale"] = locale;
            ViewData["Mode"] = this.mode;
            ViewData["Title"] = this.configuration.BotName;
            ViewData["UserId"] = isDirectLineMode ? this.userId : null;

            // If operating in DL Speech mode, token retrieval and renewal is handled in Views/Home/Index.cshtml.
            // If operating in Direct Line mode, do a one-time retrieval of the authentication token here. The
            // web chat component will then handle renewal of the presented token periodically.
            if (string.Equals(DirectLineSpeechMode, this.mode, StringComparison.OrdinalIgnoreCase))
            {
                ViewData["SpeechServiceRegionIdentifier"] = this.configuration.SpeechServiceRegionIdentifier;
                ViewData["SpeechServiceSubscriptionKey"] = this.configuration.SpeechServiceSubscriptionKey;
            }
            else if (isDirectLineMode)
            {
                string directLineToken = string.Empty;

                var directLineClient = new HttpClient();
                directLineClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                    "Bearer",
                    this.configuration.DirectLineSecret);

                string content = this.configuration.EnableDirectLineEnhancedAuthentication
                    ? JsonConvert.SerializeObject(
                        new
                        {
                            User = new
                            {
                                Id = this.userId
                            }
                        })
                    : string.Empty;

                HttpResponseMessage response = directLineClient.PostAsync(
                    GenerateDirectLineTokenUrl,
                    new StringContent(content, Encoding.UTF8, "application/json")).Result;

                if (response.IsSuccessStatusCode)
                {
                    string responseString = response.Content.ReadAsStringAsync().Result;
                    var directLineResponse = JsonConvert.DeserializeObject<DirectLineResponse>(responseString);
                    directLineToken = directLineResponse.Token;
                }

                ViewData["DirectLineToken"] = directLineToken;
            }

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
