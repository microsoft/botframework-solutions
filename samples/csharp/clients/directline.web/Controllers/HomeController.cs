// Copyright(c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
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

        private string botName;
        private string directLineSecret;
        private string mode;
        private string speechServiceRegionIdentifier;
        private string speechServiceSubscriptionKey;

        public HomeController(IConfiguration configuration)
        {
            // Retrieve the Bot configuration
            this.botName = configuration.GetSection("BotName").Value;
            this.directLineSecret = configuration.GetSection("DirectLineSecret").Value;
            this.speechServiceRegionIdentifier = configuration.GetSection("SpeechServiceRegionIdentfier").Value;
            this.speechServiceSubscriptionKey = configuration.GetSection("SpeechServiceSubscriptionKey").Value;

            // The method in which authentication token retrieval and renewal is handled, as well as configuration
            // for the web chat component, is dependent upon whether Direct Line or Direct Line Speech is being
            // utilized.
            //
            // Determine which mode to operate under:
            // - Direct Line Speech should be used if a speech service region identifier and key are provided
            // - Direct Line should be used if a DL secret is provided
            // - Default to an unknown state (i.e. invalid app configuration provided)
            if (!string.IsNullOrEmpty(this.speechServiceRegionIdentifier) &&
                !string.IsNullOrEmpty(this.speechServiceSubscriptionKey))
            {
                this.mode = DirectLineSpeechMode;
            }
            else if (!string.IsNullOrEmpty(this.directLineSecret))
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
            ViewData["Locale"] = locale;
            ViewData["Mode"] = this.mode;
            ViewData["Title"] = this.botName;

            // If operating in DL Speech mode, token retrieval and renewal is handled in Views/Home/Index.cshtml.
            // If operating in Direct Line mode, do a one-time retrieval of the authentication token here. The
            // web chat component will then handle renewal of the presented token periodically.
            if (string.Equals(DirectLineSpeechMode, this.mode, StringComparison.OrdinalIgnoreCase))
            {
                ViewData["SpeechServiceRegionIdentifier"] = this.speechServiceRegionIdentifier;
                ViewData["SpeechServiceSubscriptionKey"] = this.speechServiceSubscriptionKey;
            }
            else if (string.Equals(DirectLineMode, this.mode, StringComparison.OrdinalIgnoreCase))
            {
                string directLineToken = string.Empty;

                var directLineClient = new HttpClient();
                directLineClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", this.directLineSecret);

                HttpResponseMessage response = directLineClient.PostAsync(
                    GenerateDirectLineTokenUrl,
                    new StringContent(string.Empty, Encoding.UTF8, "application/json")).Result;

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
