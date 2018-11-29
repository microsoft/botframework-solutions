using System;
using Microsoft.Bot.Solutions.Skills;

namespace EmailSkill
{
    public class GoogleClient
    {
        public string ApplicationName { get; set; }

        public string ClientId { get; set; }

        public string ClientSecret { get; set; }

        public string[] Scopes { get; set; }

        public static GoogleClient GetGoogleClient(ISkillConfiguration config)
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
    }
}