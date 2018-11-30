using System;
using System.Threading;
using System.Threading.Tasks;
using AutomotiveSkill;
using Luis;
using Microsoft.Bot.Builder;

namespace AutomotiveSkillTest.Flow.Fakes
{
    public class MockLuisRecognizer : IRecognizer
    {
        public MockLuisRecognizer()
        {
        }

        public Task<RecognizerResult> RecognizeAsync(ITurnContext turnContext, CancellationToken cancellationToken)
        {


            //    //RecognizerResult result = new RecognizerResult();
            //    //result.Entities



            //    //var luisResult = mockEmail as RecognizerResult;                

            //    //return Task.FromResult(mockResult);
            return null;
        }

        public Task<T> RecognizeAsync<T>(ITurnContext turnContext, CancellationToken cancellationToken)
            where T : IRecognizerConvert, new()
        {
            var mockResult = new T();

            var t = typeof(T);
            var text = turnContext.Activity.Text;
            if (t.Name.Equals(typeof(VehicleSettings).Name))
            {
                var mockEmail = new MockVehicleSettingsIntent(text);

                var test = mockEmail as object;
                mockResult = (T)test;
            }
            else if (t.Name.Equals(typeof(General).Name))
            {
                var mockGeneralIntent = new MockGeneralIntent(text);

                var test = mockGeneralIntent as object;
                mockResult = (T)test;
            }

            return Task.FromResult(mockResult);
        }
    }
}