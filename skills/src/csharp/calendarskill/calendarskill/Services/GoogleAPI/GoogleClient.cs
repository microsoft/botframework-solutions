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

        public static GoogleClient GetGoogleClient(Dictionary<string, string> properties)
        {
            if (properties == null)
            {
                throw new ArgumentNullException(nameof(properties));
            }

            properties.TryGetValue("googleAppName", out var appName);
            properties.TryGetValue("googleClientId", out var clientId);
            properties.TryGetValue("googleClientSecret", out var clientSecret);
            properties.TryGetValue("googleScopes", out var scopes);

            var googleClient = new GoogleClient
            {
                ApplicationName = appName as string,
                ClientId = clientId as string,
                ClientSecret = clientSecret as string,
                Scopes = (scopes as string).Split(" "),
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