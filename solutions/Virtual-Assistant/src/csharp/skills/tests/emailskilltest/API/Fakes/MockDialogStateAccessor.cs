using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Moq;

namespace EmailSkillTest.API.Fakes
{
    public class MockDialogStateAccessor
    {
        private readonly Mock<IStatePropertyAccessor<DialogState>> mockDialogStateAccessor;

        public MockDialogStateAccessor()
        {
            this.mockDialogStateAccessor = new Mock<IStatePropertyAccessor<DialogState>>();
            this.InitializeDefaultData();
            this.SetMockBehavior();
        }

        public DialogState MockDialogState { get; set; }

        public void InitializeDefaultData()
        {
            this.MockDialogState = new DialogState();
        }

        public void SetMockBehavior()
        {
            this.mockDialogStateAccessor.Setup(f => f.GetAsync(It.IsAny<ITurnContext>(), null, default(CancellationToken))).Returns(Task.FromResult(this.MockDialogState));
        }

        public Mock<IStatePropertyAccessor<DialogState>> GetMock()
        {
            return this.mockDialogStateAccessor;
        }
    }
}
