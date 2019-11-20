// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.AI.Luis;
using Microsoft.Bot.Builder.Dialogs;
using PointOfInterestSkill.Tests.Flow.Utterances;

namespace PointOfInterestSkill.Tests.Flow.Fakes
{
    public class MockPointOfInterestLuisRecognizer : LuisRecognizer
    {
        private static LuisApplication mockApplication = new LuisApplication()
        {
            ApplicationId = "testappid",
            Endpoint = "testendpoint",
            EndpointKey = "testendpointkey"
        };

        private BaseTestUtterances poiUtterancesManager;

        public MockPointOfInterestLuisRecognizer()
            : base(application: mockApplication)
        {
            this.poiUtterancesManager = new BaseTestUtterances();
        }

        public MockPointOfInterestLuisRecognizer(BaseTestUtterances utterancesManager)
            : base(application: mockApplication)
        {
            this.poiUtterancesManager = utterancesManager;
        }

        public MockPointOfInterestLuisRecognizer(params BaseTestUtterances[] utterancesManagers)
            : base(application: mockApplication)
        {
            this.poiUtterancesManager = new BaseTestUtterances();

            foreach (var manager in utterancesManagers)
            {
                foreach (var pair in manager)
                {
                    this.poiUtterancesManager.TryAdd(pair.Key, pair.Value);
                }
            }
        }

        public void AddUtteranceManager(BaseTestUtterances utterancesManager)
        {
            this.poiUtterancesManager.AddManager(utterancesManager);
        }

        public override Task<T> RecognizeAsync<T>(ITurnContext turnContext, CancellationToken cancellationToken)
        {
            var text = turnContext.Activity.Text;
            var mockEmail = poiUtterancesManager.GetValueOrDefault(text, poiUtterancesManager.GetBaseNoneIntent());

            var test = mockEmail as object;
            var mockResult = (T)test;

            return Task.FromResult(mockResult);
        }
    }
}
