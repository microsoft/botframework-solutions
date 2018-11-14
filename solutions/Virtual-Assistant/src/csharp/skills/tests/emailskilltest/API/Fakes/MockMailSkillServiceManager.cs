using System;
using EmailSkill;
using Microsoft.Graph;

namespace EmailSkillTest.API.Fakes
{
    public class MockMailSkillServiceManager : IMailSkillServiceManager
    {
        public IMailService InitMailService(string token, TimeZoneInfo timeZoneInfo)
        {
            var mockGraphServiceClient = new MockGraphServiceClientGen();
            IGraphServiceClient serviceClient = mockGraphServiceClient.GetMockGraphServiceClient().Object;
            MailService mailService = new MailService(serviceClient, timeZoneInfo: TimeZoneInfo.Local);

            return mailService;
        }

        public IUserService InitUserService(string token, TimeZoneInfo timeZoneInfo)
        {
            var mockGraphServiceClient = new MockGraphServiceClientGen();
            IGraphServiceClient serviceClient = mockGraphServiceClient.GetMockGraphServiceClient().Object;
            UserService userService = new UserService(serviceClient, timeZoneInfo: TimeZoneInfo.Local);

            return userService;
        }
    }
}
