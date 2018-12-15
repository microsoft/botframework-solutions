using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CalendarSkill;
using Moq;

namespace CalendarSkillTest.API.Fakes
{
    public static class MockBaseUserClient
    {
        public static Mock<IUserService> mockBaseUserService;

        static MockBaseUserClient()
        {
            mockBaseUserService = new Mock<IUserService>();
            mockBaseUserService.Setup(service => service.GetPeopleAsync(It.IsAny<string>())).Returns((string name) =>
            {
                return Task.FromResult(new List<PersonModel>());
            });
            mockBaseUserService.Setup(service => service.GetUserAsync(It.IsAny<string>())).Returns((string name) =>
            {
                return Task.FromResult(new List<PersonModel>());
            });
            mockBaseUserService.Setup(service => service.GetContactsAsync(It.IsAny<string>())).Returns((string name) =>
            {
                return Task.FromResult(new List<PersonModel>());
            });
        }
    }
}
