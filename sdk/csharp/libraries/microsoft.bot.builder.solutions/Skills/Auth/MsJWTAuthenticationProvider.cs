// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace Microsoft.Bot.Builder.Solutions.Skills.Auth
{
    [Obsolete("This type is being deprecated. To continue using Skill capability please refer to https://aka.ms/botframework-solutions/releases/0_8", false)]
    public class MsJWTAuthenticationProvider : IAuthenticationProvider
    {
        private OpenIdConnectConfiguration _openIdConfig;
        private readonly string _microsoftAppId;
        private readonly string _openIdMetadataUrl = "https://login.microsoftonline.com/common/v2.0/.well-known/openid-configuration";

        public MsJWTAuthenticationProvider(string microsoftAppId, string openIdMetadataUrl = null)
        {
            _microsoftAppId = microsoftAppId;
            if (!string.IsNullOrWhiteSpace(openIdMetadataUrl))
            {
                _openIdMetadataUrl = openIdMetadataUrl;
            }
        }

        public async Task<ClaimsIdentity> AuthenticateAsync(string authHeader)
        {
            if (string.IsNullOrWhiteSpace(_microsoftAppId))
            {
                throw new ArgumentNullException("microsoftAppId");
            }

            if (_openIdConfig == null)
            {
                var configurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(_openIdMetadataUrl, new OpenIdConnectConfigurationRetriever());
                _openIdConfig = await configurationManager.GetConfigurationAsync(CancellationToken.None).ConfigureAwait(false);
            }

            try
            {
                var validationParameters =
                    new TokenValidationParameters
                    {
                        ValidateIssuer = false, // do not validate issuer
                        ValidAudiences = new[] { _microsoftAppId },
                        IssuerSigningKeys = _openIdConfig.SigningKeys,
                    };

                var handler = new JwtSecurityTokenHandler();
                var user = handler.ValidateToken(authHeader.Replace("Bearer ", string.Empty), validationParameters, out var validatedToken);

                return user.Identities.OfType<ClaimsIdentity>().FirstOrDefault();
            }
            catch
            {
                return null;
            }
        }
    }
}