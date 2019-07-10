using System;
using System.Collections.Generic;
using Google;
using Microsoft.Bot.Builder.Skills;

namespace CalendarSkill.Services.GoogleAPI
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

            var appName = settings.GoogleAppName;
            var clientId = settings.GoogleClientId;
            var clientSecret = settings.GoogleClientSecret;
            var scopes = settings.GoogleScopes;

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