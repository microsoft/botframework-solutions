using EmailSkill;
using Microsoft.Bot.Builder;
using Moq;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EmailSkillTest.API.Fakes
{
    public class MockEmailStateAccessor
    {
        private readonly Mock<IStatePropertyAccessor<EmailSkillState>> mockEmailStateAccessor;

        public MockEmailStateAccessor()
        {
            this.mockEmailStateAccessor = new Mock<IStatePropertyAccessor<EmailSkillState>>();
            this.InitializeDefaultData();
            this.SetMockBehavior();
        }

        public EmailSkillState MockEmailSkillState { get; set; }

        public void InitializeDefaultData()
        {
            this.MockEmailSkillState = new EmailSkillState();
            this.MockEmailSkillState.Recipients = new List<Microsoft.Graph.Recipient>();
        }

        public void SetMockBehavior()
        {
            this.mockEmailStateAccessor.Setup(f => f.GetAsync(It.IsAny<ITurnContext>(), null, default(CancellationToken))).Returns(Task.FromResult(this.MockEmailSkillState));
        }

        public Mock<IStatePropertyAccessor<EmailSkillState>> GetMock()
        {
            return this.mockEmailStateAccessor;
        }
    }
}
