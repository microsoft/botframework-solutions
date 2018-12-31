using System;
using Google;
using Microsoft.Bot.Solutions.Skills;

namespace CalendarSkill.ServiceClients.GoogleAPI
{
    public class GoogleClient
    {
        private const string APIErrorAccessDenied = "insufficient permission";

        public string ApplicationName { get; set; }

        public string ClientId { get; set; }

        public string ClientSecret { get; set; }

        public string[] Scopes { get; set; }

        public static GoogleClient GetGoogleClient(SkillConfigurationBase config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            config.Properties.TryGetValue("googleAppName", out object appName);
            config.Properties.TryGetValue("googleClientId", out object clientId);
            config.Properties.TryGetValue("googleClientSecret", out object clientSecret);
            config.Properties.TryGetValue("googleScopes", out object scopes);

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