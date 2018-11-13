using System;
using System.Threading;
using System.Threading.Tasks;
using Luis;
using Microsoft.Bot.Builder;

namespace ToDoSkillTest.Flow.Fakes
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
            var text = turnContext.Activity.Text;
            if (t.Name.Equals(typeof(ToDo).Name))
            {
                MockToDoIntent mockToDo = new MockToDoIntent(text);

                var test = mockToDo as object;
                mockResult = (T)test;
            }
            else if (t.Name.Equals(typeof(General).Name))
            {
                MockGeneralIntent mockGeneralIntent = new MockGeneralIntent(text);

                var test = mockGeneralIntent as object;
                mockResult = (T)test;
            }

            return mockResult;
        }
    }
}