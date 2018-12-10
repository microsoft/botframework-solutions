using System;
using EmailSkill;

namespace EmailSkillTest.Flow.Fakes
{
    public class MockServiceManager : IServiceManager
    {
        public MockMailService MockMailService;

        public MockUserService MockUserService;

        public MockServiceManager()
        {
            MockMailService = new MockMailService();
            MockUserService = new MockUserService();
        }

        public IMailService InitMailService(string token, TimeZoneInfo timeZoneInfo, MailSource mailSource)
        {
            return MockMailService;
        }

        public IUserService InitUserService(string token, TimeZoneInfo timeZoneInfo, MailSource mailSource)
        {
            return MockUserService;
        }
    }
}
