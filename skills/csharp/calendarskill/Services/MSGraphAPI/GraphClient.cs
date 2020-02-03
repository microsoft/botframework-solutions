// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.Bot.Solutions.Skills;
using Microsoft.Graph;

namespace CalendarSkill.Services.MSGraphAPI
{
    public class GraphClient
    {
        private const string APIErrorAccessDenied = "erroraccessdenied";

        public static GraphServiceClient GetAuthenticatedClient(string accessToken)
        {
            var graphClient = new GraphServiceClient(
                new DelegateAuthenticationProvider(
                    async (requestMessage) =>
                    {
                        // Append the access token to the request.
                        requestMessage.Headers.Authorization = new AuthenticationHeaderValue("bearer", accessToken);

                        // Get event times in the current time zone.
                        requestMessage.Headers.Add("Prefer", "outlook.timezone=\"" + TimeZoneInfo.Utc.Id + "\"");
                        await Task.CompletedTask;
                    }));
            return graphClient;
        }

        public static SkillException HandleGraphAPIException(ServiceException ex)
        {
            var skillExceptionType = SkillExceptionType.Other;
            if (ex.Message.Contains(APIErrorAccessDenied, StringComparison.InvariantCultureIgnoreCase))
            {
                skillExceptionType = SkillExceptionType.APIAccessDenied;
            }
            else if (ex.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                skillExceptionType = SkillExceptionType.APIUnauthorized;
            }
            else if (ex.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                skillExceptionType = SkillExceptionType.APIForbidden;
            }
            else if (ex.StatusCode == System.Net.HttpStatusCode.BadRequest)
            {
                skillExceptionType = SkillExceptionType.APIBadRequest;
            }

            return new SkillException(skillExceptionType, ex.Message, ex);
        }
    }
}
