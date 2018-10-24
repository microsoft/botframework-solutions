using Luis;
using Microsoft.Bot.Builder;
using Microsoft.Graph;
using Moq;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static Luis.Email;

namespace EmailSkillTest.Flow.Fakes
{
    public class MockLuisRecognizer : IRecognizer
    {
        public MockLuisRecognizer()
        {

        }

        public Task<RecognizerResult> RecognizeAsync(ITurnContext turnContext, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public async Task<T> RecognizeAsync<T>(ITurnContext turnContext, CancellationToken cancellationToken)
            where T : IRecognizerConvert, new()
        {
            T mockResult = new T();

            Type t = typeof(T);
            if (t.Name.Equals(typeof(Email).Name))
            {
                MockEmail mockEmail = new MockEmail(turnContext.Activity.Text);

                var test = mockEmail as object;
                mockResult = (T)test;
            }
            else if (t.Name.Equals(typeof(General).Name))
            {
                var generalGen = new Mock<General>();
                generalGen.Setup(f => f.TopIntent()).Returns((General.Intent.None, 0.90));
                generalGen.SetupGet(f => f.Entities).Returns(new General._Entities());

                var test = generalGen.Object as object;
                mockResult = (T)test;
            }

            return mockResult;
        }
    }
}
