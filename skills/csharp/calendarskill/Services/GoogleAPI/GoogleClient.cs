// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Google;
using Microsoft.Bot.Solutions.Skills;

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
                Scopes = scopes.Split(","),
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
            else if (ex.HttpStatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                skillExceptionType = SkillExceptionType.APIUnauthorized;
            }
            else if (ex.HttpStatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                skillExceptionType = SkillExceptionType.APIForbidden;
            }
            else if (ex.HttpStatusCode == System.Net.HttpStatusCode.BadRequest)
            {
                skillExceptionType = SkillExceptionType.APIBadRequest;
            }

            return new SkillException(skillExceptionType, ex.Message, ex);
        }
    }
}