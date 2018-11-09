using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Graph;
using Moq;

namespace CalendarSkillTest.API.Fakes
{
    public class MockGraphServiceClientGen
    {
        private readonly Mock<IGraphServiceClient> mockCalendarService;

        public MockGraphServiceClientGen()
        {
            this.mockCalendarService = new Mock<IGraphServiceClient>();
            this.InitializeDefaultData();
            this.SetMockBehavior();
        }

        public User Me { get; set; }


        public IGraphServiceUsersCollectionPage Users { get; set; }

        public IUserPeopleCollectionPage People { get; set; }

        public void InitializeDefaultData()
        {
            this.Me = new User();
            Me.Mail = "test@test.com";
            Me.DisplayName = "Test Test";

            this.Users = new GraphServiceUsersCollectionPage();

            this.People = new UserPeopleCollectionPage();
        }

        public void SetMockBehavior()
        {
            // Mock user service behavior
            this.MockGetUserAsync();
            this.MockGetPeopleAsync();
        }

        public Mock<IGraphServiceClient> GetMockGraphServiceClient()
        {
            return this.mockCalendarService;
        }

        private void MockGetUserAsync()
        {
            this.mockCalendarService.Setup(f => f.Users.Request(It.IsAny<List<QueryOption>>()).GetAsync()).Returns(Task.FromResult(this.Users));
        }

        private void MockGetPeopleAsync()
        {
            this.mockCalendarService.Setup(f => f.Me.People.Request(It.IsAny<List<QueryOption>>()).GetAsync()).Returns(Task.FromResult(this.People));
        }
    }
}
