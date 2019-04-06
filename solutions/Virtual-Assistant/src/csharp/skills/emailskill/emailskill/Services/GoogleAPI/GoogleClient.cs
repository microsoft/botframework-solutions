using System;
using EmailSkill.Services;
using Google;
using Microsoft.Bot.Builder.Solutions.Skills;

namespace EmailSkill.ServiceClients.GoogleAPI
{
    public class GoogleClient
    {
        private const string APIErrorAccessDenied = "insufficient permission";

        public string ApplicationName { get; set; }

        public string ClientId { get; set; }

        public string ClientSecret { get; set; }

        public string[] Scopes { get; set; }

        public static GoogleClient GetGoogleClient(BotSettings settings)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            settings.Properties.TryGetValue("googleAppName", out var appName);
            settings.Properties.TryGetValue("googleClientId", out var clientId);
            settings.Properties.TryGetValue("googleClientSecret", out var clientSecret);
            settings.Properties.TryGetValue("googleScopes", out var scopes);

            var googleClient = new GoogleClient
            {
                ApplicationName = appName,
                ClientId = clientId,
                ClientSecret = clientSecret,
                Scopes = scopes.Split(" "),
            };

            return googleClient;
        }

        public static SkillException HandleGoogleAPIException(GoogleApiException ex)
        {
            var skillExceptionType = SkillExceptionType.Other;

            if (ex.Error.Message.Equals(APIErrorAccessDenied, StringComparison.InvariantCultureIgnoreCase))
            {
                skillExceptionType = SkillExceptionType.APIAccessDenied;
            }

            return new SkillException(skillExceptionType, ex.Message, ex);
        }
    }
}