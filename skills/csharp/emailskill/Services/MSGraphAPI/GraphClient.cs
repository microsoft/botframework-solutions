// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.Bot.Solutions.Skills;
using Microsoft.Graph;

namespace EmailSkill.Services.MSGraphAPI
{
    public class GraphClient
    {
        private const string APIErrorAccessDenied = "erroraccessdenied";
        private const string APIErrorMessageSubmissionBlocked = "errormessagesubmissionblocked";

        /// <summary>
        /// Get an authenticated ms graph client use access token.
        /// </summary>
        /// <param name="accessToken">access token.</param>
        /// <param name="timeZoneInfo">timeZone info.</param>
        /// <returns>Authenticated graph service client.</returns>
        public static IGraphServiceClient GetAuthenticatedClient(string accessToken, TimeZoneInfo timeZoneInfo)
        {
            GraphServiceClient graphClient = new GraphServiceClient(
                new DelegateAuthenticationProvider(
                    async (requestMessage) =>
                    {
                        // Append the access token to the request.
                        requestMessage.Headers.Authorization = new AuthenticationHeaderValue("bearer", accessToken);

                        // Get event times in the current time zone.
                        requestMessage.Headers.Add("Prefer", "outlook.timezone=\"" + timeZoneInfo.Id + "\"");
                        requestMessage.Headers.Add("Prefer", "outlook.body-content-type=\"text\"");

                        await Task.CompletedTask;
                    }));
            return graphClient;
        }

        public static SkillException HandleGraphAPIException(ServiceException ex)
        {
            var skillExceptionType = SkillExceptionType.Other;

            if (ex.Error.Code.Equals(APIErrorAccessDenied, StringComparison.InvariantCultureIgnoreCase))
            {
                skillExceptionType = SkillExceptionType.APIAccessDenied;
            }
            else if (ex.Error.Code.Equals(APIErrorMessageSubmissionBlocked, StringComparison.InvariantCultureIgnoreCase))
            {
                skillExceptionType = SkillExceptionType.AccountNotActivated;
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