﻿using System;
using System.Threading;
using System.Threading.Tasks;
using Luis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Solutions.Telemetry;

namespace AutomotiveSkillTest.Flow.Fakes
{
    public class MockLuisRecognizer : ITelemetryLuisRecognizer
    {
        public MockLuisRecognizer()
        {
        }
        public bool LogPersonalInformation => throw new NotImplementedException();

        public Task<RecognizerResult> RecognizeAsync(ITurnContext turnContext, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<T> RecognizeAsync<T>(ITurnContext turnContext, CancellationToken cancellationToken)
            where T : IRecognizerConvert, new()
        {
            var mockResult = new T();

            var t = typeof(T);
            var text = turnContext.Activity.Text;
            if (t.Name.Equals(typeof(VehicleSettingsLuis).Name))
            {
                var mockVehicle = new MockVehicleSettingsIntent(text);

                var test = mockVehicle as object;
                mockResult = (T)test;
            }
            else if (t.Name.Equals(typeof(VehicleSettingsNameSelectionLuis).Name))
            {
                var mockVehicleNameIntent = new MockVehicleSettingsNameIntent(text);

                var test = mockVehicleNameIntent as object;
                mockResult = (T)test;
            }
            else if (t.Name.Equals(typeof(VehicleSettingsValueSelectionLuis).Name))
            {
                var mockVehicleValueIntent = new MockVehicleSettingsValueIntent(text);

                var test = mockVehicleValueIntent as object;
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

        public Task<T> RecognizeAsync<T>(DialogContext dialogContext, CancellationToken cancellationToken = default(CancellationToken))
            where T : IRecognizerConvert, new()
        {
            throw new NotImplementedException();
        }
    }
}