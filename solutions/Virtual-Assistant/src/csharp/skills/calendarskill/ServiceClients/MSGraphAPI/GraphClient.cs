using System;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.Bot.Solutions.Skills;
using Microsoft.Graph;

namespace CalendarSkill.ServiceClients.MSGraphAPI
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

            return new SkillException(skillExceptionType, ex.Message, ex);
        }
    }
}
