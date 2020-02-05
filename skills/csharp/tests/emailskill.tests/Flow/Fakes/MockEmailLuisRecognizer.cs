// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EmailSkill.Tests.Flow.Utterances;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.AI.Luis;

namespace EmailSkill.Tests.Flow.Fakes
{
    public class MockEmailLuisRecognizer : LuisRecognizer
    {
        private static LuisApplication mockApplication = new LuisApplication()
        {
            ApplicationId = "testappid",
            Endpoint = "testendpoint",
            EndpointKey = "testendpointkey"
        };

        private BaseTestUtterances emailUtterancesManager;

        public MockEmailLuisRecognizer()
            : base(application: mockApplication)
        {
            this.emailUtterancesManager = new BaseTestUtterances();
        }

        public MockEmailLuisRecognizer(BaseTestUtterances utterancesManager)
            : base(application: mockApplication)
        {
            this.emailUtterancesManager = utterancesManager;
        }

        public MockEmailLuisRecognizer(params BaseTestUtterances[] utterancesManagers)
            : base(application: mockApplication)
        {
            this.emailUtterancesManager = new BaseTestUtterances();

            foreach (var manager in utterancesManagers)
            {
                foreach (var pair in manager)
                {
                    this.emailUtterancesManager.TryAdd(pair.Key, pair.Value);
                }
            }
        }

        public bool LogPersonalInformation { get; set; } = false;

        public IBotTelemetryClient TelemetryClient { get; set; } = new NullBotTelemetryClient();

        public void AddUtteranceManager(BaseTestUtterances utterancesManager)
        {
            this.emailUtterancesManager.AddManager(utterancesManager);
        }

        public override Task<T> RecognizeAsync<T>(ITurnContext turnContext, CancellationToken cancellationToken)
        {
            var text = turnContext.Activity.Text;

            var mockEmail = emailUtterancesManager.GetValueOrDefault(text, emailUtterancesManager.GetBaseNoneIntent());

            var test = mockEmail as object;
            var mockResult = (T)test;
            return Task.FromResult((T)mockResult);
        }
    }
}