using System;
using EmailSkill.Model;
using EmailSkill.ServiceClients;
using EmailSkill.ServiceClients.MSGraphAPI;
using EmailSkillTest.API.Fakes.MSGraph;
using Microsoft.Graph;

namespace EmailSkillTest.API.Fakes
{
    public class MockServiceManager : IServiceManager
    {
        public IMailService InitMailService(string token, TimeZoneInfo timeZoneInfo, MailSource source)
        {
            var mockGraphServiceClient = new MockGraphServiceClient();
            IGraphServiceClient serviceClient = mockGraphServiceClient.GetMockGraphServiceClient().Object;
            MSGraphMailAPI mailService = new MSGraphMailAPI(serviceClient, timeZoneInfo: TimeZoneInfo.Local);

            return mailService;
        }

        public IUserService InitUserService(string token, TimeZoneInfo timeZoneInfo, MailSource source)
        {
            var mockGraphServiceClient = new MockGraphServiceClient();
            IGraphServiceClient serviceClient = mockGraphServiceClient.GetMockGraphServiceClient().Object;
            MSGraphUserService userService = new MSGraphUserService(serviceClient, timeZoneInfo: TimeZoneInfo.Local);

            return userService;
        }
    }
}