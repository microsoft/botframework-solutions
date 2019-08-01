using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Threading;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace Microsoft.Bot.Builder.Skills.Auth
{
    public class MsJWTAuthenticationProvider : IAuthenticationProvider
    {
        private const string OpenIdMetadataUrl = "https://login.microsoftonline.com/common/v2.0/.well-known/openid-configuration";

        private static OpenIdConnectConfiguration openIdConfig;
        private readonly string _microsoftAppId;
        private List<string> _jwtIssuers = new List<string>
        {
            "https://login.microsoftonline.com/72f988bf-86f1-41af-91ab-2d7cd011db47/v2.0",
            "https://sts.windows.net/72f988bf-86f1-41af-91ab-2d7cd011db47/",
        };

        static MsJWTAuthenticationProvider()
        {
            var configurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(OpenIdMetadataUrl, new OpenIdConnectConfigurationRetriever());
            openIdConfig = configurationManager.GetConfigurationAsync(CancellationToken.None).GetAwaiter().GetResult();
        }

        public MsJWTAuthenticationProvider(string microsoftAppId)
        {
            _microsoftAppId = !string.IsNullOrWhiteSpace(microsoftAppId) ? microsoftAppId : throw new ArgumentNullException(nameof(microsoftAppId));
        }

        public bool Authenticate(string authHeader)
        {
            try
            {
                var validationParameters =
                    new TokenValidationParameters
                    {
                        ValidIssuers = _jwtIssuers,
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