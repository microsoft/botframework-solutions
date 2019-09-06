// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace Microsoft.Bot.Builder.Skills.Auth
{
    public class MSJwtAuthenticationProvider : IAuthenticationProvider
    {
        private static OpenIdConnectConfiguration _openIdConfig;
        private readonly string _microsoftAppId;
        private readonly string _openIdMetadataUrl = "https://login.microsoftonline.com/common/v2.0/.well-known/openid-configuration";

        public MSJwtAuthenticationProvider(string microsoftAppId, string openIdMetadataUrl = null)
        {
            _microsoftAppId = !string.IsNullOrWhiteSpace(microsoftAppId) ? microsoftAppId : throw new ArgumentNullException(nameof(microsoftAppId));
            if (!string.IsNullOrWhiteSpace(openIdMetadataUrl))
            {
                _openIdMetadataUrl = openIdMetadataUrl;
            }

            var configurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(_openIdMetadataUrl, new OpenIdConnectConfigurationRetriever());
            _openIdConfig = configurationManager.GetConfigurationAsync(CancellationToken.None).GetAwaiter().GetResult();
        }

        public ClaimsIdentity Authenticate(string authHeader)
        {
            if (authHeader == null)
            {
                throw new ArgumentNullException(nameof(authHeader));
            }

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = false, // do not validate issuer
                ValidAudiences = new[] { _microsoftAppId },
                IssuerSigningKeys = _openIdConfig.SigningKeys,
            };

            var handler = new JwtSecurityTokenHandler();
            var user = handler.ValidateToken(authHeader.Replace("Bearer ", string.Empty), validationParameters, out _);

            return user.Identities.FirstOrDefault();
        }
    }
}
