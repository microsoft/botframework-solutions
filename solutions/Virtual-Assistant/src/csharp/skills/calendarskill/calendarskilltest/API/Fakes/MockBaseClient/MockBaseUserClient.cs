using System.Collections.Generic;
using System.Threading.Tasks;
using CalendarSkill.Models;
using CalendarSkill.ServiceClients;
using Moq;

namespace CalendarSkillTest.API.Fakes.MockBaseClient
{
    public static class MockBaseUserClient
    {
        private static Mock<IUserService> mockBaseUserService;

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

        public static IUserService GetUserService()
        {
            return mockBaseUserService.Object;
        }
    }
}
