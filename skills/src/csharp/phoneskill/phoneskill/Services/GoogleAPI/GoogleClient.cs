using System;
using Google;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Util.Store;
using Microsoft.Bot.Builder.Skills;
using PhoneSkill.Services;

namespace PhoneSkill.Services.GoogleAPI
{
    public class GoogleClient
    {
        private const string APIErrorAccessDenied = "insufficient permission";

        public GoogleClient(BotSettings settings, string token)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            Token = token;

            settings.Properties.TryGetValue("googleAppName", out var appName);
            settings.Properties.TryGetValue("googleClientId", out var clientId);
            settings.Properties.TryGetValue("googleClientSecret", out var clientSecret);
            settings.Properties.TryGetValue("googleScopes", out var scopes);

            ApplicationName = appName as string;
            ClientId = clientId as string;
            ClientSecret = clientSecret as string;
            Scopes = (scopes as string).Split(" ");
        }

        public string Token { get; set; }

        public string ApplicationName { get; set; }

        public string ClientId { get; set; }

        public string ClientSecret { get; set; }

        public string[] Scopes { get; set; }

        public static SkillException HandleGoogleAPIException(GoogleApiException ex)
        {
            var skillExceptionType = SkillExceptionType.Other;

            if (ex.Error.Message.Equals(APIErrorAccessDenied, StringComparison.InvariantCultureIgnoreCase))
            {
                skillExceptionType = SkillExceptionType.APIAccessDenied;
            }

            return new SkillException(skillExceptionType, ex.Message, ex);
        }

        public UserCredential GetCredential()
        {
            GoogleAuthorizationCodeFlow flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
            {
                ClientSecrets = new ClientSecrets
                {
                    ClientId = ClientId,
                    ClientSecret = ClientSecret,
                },
                Scopes = Scopes,
                DataStore = new FileDataStore("Store"),
            });

            TokenResponse tokenRes = new TokenResponse
            {
                AccessToken = Token,
                ExpiresInSeconds = 3600,
                IssuedUtc = DateTime.UtcNow,
            };

            return new UserCredential(flow, Environment.UserName, tokenRes);
        }
    }
}
