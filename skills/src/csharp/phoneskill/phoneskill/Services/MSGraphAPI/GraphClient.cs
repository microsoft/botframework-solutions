using System;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Skills;
using Microsoft.Graph;

namespace PhoneSkill.Services.MSGraphAPI
{
    public class GraphClient
    {
        private const string APIErrorAccessDenied = "erroraccessdenied";
        private const string APIErrorMessageSubmissionBlocked = "errormessagesubmissionblocked";

        /// <summary>
        /// Get an authenticated ms graph client use access token.
        /// </summary>
        /// <param name="accessToken">access token.</param>
        /// <returns>Authenticated graph service client.</returns>
        public static IGraphServiceClient GetAuthenticatedClient(string accessToken)
        {
            GraphServiceClient graphClient = new GraphServiceClient(
                new DelegateAuthenticationProvider(
                    async (requestMessage) =>
                    {
                        // Append the access token to the request.
                        requestMessage.Headers.Authorization = new AuthenticationHeaderValue("bearer", accessToken);

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

            return new SkillException(skillExceptionType, ex.Message, ex);
        }
    }
}
