using System;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.Bot.Solutions.Skills;
using Microsoft.Graph;

namespace EmailSkill.ServiceClients.MSGraphAPI
{
    public class GraphClient
    {
        private const string APIErrorAccessDenied = "erroraccessdenied";

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

            return new SkillException(skillExceptionType, ex.Message, ex);
        }
    }
}