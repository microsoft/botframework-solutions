using System;
using System.IdentityModel.Tokens.Jwt;
using System.Threading;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace Microsoft.Bot.Builder.Skills.Auth
{
    public class MsJWTAuthenticationProvider : IAuthenticationProvider
    {
        private static OpenIdConnectConfiguration openIdConfig;
        private readonly string _microsoftAppId;
        private readonly string _openIdMetadataUrl = "https://login.microsoftonline.com/common/v2.0/.well-known/openid-configuration";

        public MsJWTAuthenticationProvider(string microsoftAppId, string openIdMetadataUrl = null)
        {
            _microsoftAppId = !string.IsNullOrWhiteSpace(microsoftAppId) ? microsoftAppId : throw new ArgumentNullException(nameof(microsoftAppId));
            if (!string.IsNullOrWhiteSpace(openIdMetadataUrl))
            {
                _openIdMetadataUrl = openIdMetadataUrl;
            }

            var configurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(_openIdMetadataUrl, new OpenIdConnectConfigurationRetriever());
            openIdConfig = configurationManager.GetConfigurationAsync(CancellationToken.None).GetAwaiter().GetResult();
        }

        public bool Authenticate(string authHeader)
        {
            try
            {
                var validationParameters =
                    new TokenValidationParameters
                    {
                        ValidateIssuer = false, // do not validate issuer
                        ValidAudiences = new[] { _microsoftAppId },
                        IssuerSigningKeys = openIdConfig.SigningKeys,
                    };

                var handler = new JwtSecurityTokenHandler();
                var user = handler.ValidateToken(authHeader.Replace("Bearer ", string.Empty), validationParameters, out var validatedToken);

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}