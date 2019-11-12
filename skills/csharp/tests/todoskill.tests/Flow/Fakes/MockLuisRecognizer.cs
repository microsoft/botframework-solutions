// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Luis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.AI.Luis;
using Microsoft.Bot.Builder.Dialogs;
using ToDoSkill.Tests.Flow.Utterances;

namespace ToDoSkill.Tests.Flow.Fakes
{
    public class MockLuisRecognizer : LuisRecognizer
    {
        private static LuisApplication mockApplication = new LuisApplication()
        {
            ApplicationId = "testappid",
            Endpoint = "testendpoint",
            EndpointKey = "testendpointkey"
        };

        private BaseTestUtterances utterancesManager;
        private GeneralTestUtterances generalUtterancesManager;

        public MockLuisRecognizer(BaseTestUtterances utterancesManager)
            : base(mockApplication)
        {
            this.utterancesManager = utterancesManager;
        }

        public MockLuisRecognizer(params BaseTestUtterances[] utterancesManagers)
            : base(mockApplication)
        {
            this.utterancesManager = new BaseTestUtterances();

            foreach (var manager in utterancesManagers)
            {
                foreach (var pair in manager)
                {
                    this.utterancesManager.TryAdd(pair.Key, pair.Value);
                }
            }
        }

        public MockLuisRecognizer(GeneralTestUtterances generalUtterancesMananger)
            : base(mockApplication)
        {
            this.generalUtterancesManager = generalUtterancesMananger;
        }

        public override Task<T> RecognizeAsync<T>(ITurnContext turnContext, CancellationToken cancellationToken)
        {
            var mockResult = new T();

            var t = typeof(T);
            var text = turnContext.Activity.Text;
            if (t.Name.Equals(typeof(ToDoLuis).Name))
            {
                var mockToDo = utterancesManager.GetValueOrDefault(text, utterancesManager.GetBaseNoneIntent());

                var test = mockToDo as object;
                mockResult = (T)test;
            }
            else if (t.Name.Equals(typeof(General).Name))
            {
                var mockGeneralIntent = generalUtterancesManager.GetValueOrDefault(text, generalUtterancesManager.GetBaseNoneIntent());

                var test = mockGeneralIntent as object;
                mockResult = (T)test;
            }

            return Task.FromResult(mockResult);
        }
    }
}