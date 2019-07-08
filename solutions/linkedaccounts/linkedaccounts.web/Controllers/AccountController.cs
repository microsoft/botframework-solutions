// Copyright(c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace LinkedAccounts.Web.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Authentication;
    using Microsoft.AspNetCore.Authentication.Cookies;
    using Microsoft.AspNetCore.Authentication.OpenIdConnect;
    using Microsoft.AspNetCore.Mvc;

    /// <summary>
    /// Controller used for account linked.
    /// </summary>
    [Route("[controller]/[action]")]
    public class AccountController : Controller
    {
        [HttpGet]
        public IActionResult SignIn()
        {
            var redirectUrl = this.Url.Action(nameof(HomeController.Index), "Home");
            var authenticationProperties = new AuthenticationProperties
            {
                RedirectUri = redirectUrl
            };

            return this.Challenge(
                authenticationProperties,
                OpenIdConnectDefaults.AuthenticationScheme);
        }

        [HttpGet]
        public IActionResult SignOut()
        {
            var callbackUrl = this.Url.Action(nameof(this.SignedOut), "Account", values: null, protocol: this.Request.Scheme);
            return this.SignOut(
                new AuthenticationProperties { RedirectUri = callbackUrl },
                CookieAuthenticationDefaults.AuthenticationScheme);
        }

        [HttpGet]
        public IActionResult SignedOut()
        {
            if (this.User.Identity.IsAuthenticated)
            {
                // Redirect to home page if the user is authenticated.
                return this.RedirectToAction(nameof(HomeController.Index), "Home");
            }

            return this.View();
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return this.View();
        }
    }
}
