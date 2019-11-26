// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Microsoft.Bot.Builder.Solutions.Skills.Auth
{
    public class Authenticator : IAuthenticator
    {
        private readonly IAuthenticationProvider _authenticationProvider;
        private readonly IWhitelistAuthenticationProvider _whitelistAuthenticationProvider;

        public Authenticator(IAuthenticationProvider authenticationProvider, IWhitelistAuthenticationProvider whitelistAuthenticationProvider)
        {
            _authenticationProvider = authenticationProvider ?? throw new ArgumentNullException(nameof(authenticationProvider));
            _whitelistAuthenticationProvider = whitelistAuthenticationProvider ?? throw new ArgumentNullException(nameof(whitelistAuthenticationProvider));
        }

        public async Task<ClaimsIdentity> AuthenticateAsync(HttpRequest httpRequest, HttpResponse httpResponse)
        {
            if (httpRequest == null)
            {
                throw new ArgumentNullException(nameof(httpRequest));
            }

            if (httpResponse == null)
            {
                throw new ArgumentNullException(nameof(httpResponse));
            }

            var authorizationHeader = httpRequest.Headers["Authorization"];
            if (string.IsNullOrWhiteSpace(authorizationHeader))
            {
                httpResponse.StatusCode = (int)HttpStatusCode.Unauthorized;
                return null;
            }

            var claimsIdentity = await _authenticationProvider.AuthenticateAsync(authorizationHeader).ConfigureAwait(false);
            if (claimsIdentity == null)
            {
                httpResponse.StatusCode = (int)HttpStatusCode.Unauthorized;
                return null;
            }

            var appIdClaimName = AuthHelpers.GetAppIdClaimName(claimsIdentity);
            var appId = claimsIdentity.Claims.FirstOrDefault(c => c.Type == appIdClaimName)?.Value;
            if (_whitelistAuthenticationProvider.AppsWhitelist != null
                && _whitelistAuthenticationProvider.AppsWhitelist.Count > 0
                && !_whitelistAuthenticationProvider.AppsWhitelist.Contains(appId))
            {
                httpResponse.StatusCode = (int)HttpStatusCode.Unauthorized;
                await httpResponse.WriteAsync("Skill could not allow access from calling bot.").ConfigureAwait(false);
            }

            return claimsIdentity;
        }
    }
}