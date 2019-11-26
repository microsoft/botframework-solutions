// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HospitalitySkill.Tests.Flow.Utterances;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.AI.Luis;

namespace HospitalitySkill.Tests.Flow.Fakes
{
    public class BaseMockLuisRecognizer<TConvert> : LuisRecognizer
    {
        private static LuisApplication mockApplication = new LuisApplication()
        {
            ApplicationId = "testappid",
            Endpoint = "testendpoint",
            EndpointKey = "testendpointkey"
        };

        private readonly BaseTestUtterances<TConvert> utterancesManager;

        public BaseMockLuisRecognizer(params BaseTestUtterances<TConvert>[] utterancesManagers)
    : base(application: mockApplication)
        {
            utterancesManager = utterancesManagers[0];

            for (int i = 1; i < utterancesManagers.Length; ++i)
            {
                foreach (var pair in utterancesManagers[i])
                {
                    if (!utterancesManager.TryAdd(pair.Key, pair.Value))
                    {
                        throw new Exception($"Key:{pair.Key} already exists!");
                    }
                }
            }
        }

        public override Task<T> RecognizeAsync<T>(ITurnContext turnContext, CancellationToken cancellationToken)
        {
            var text = turnContext.Activity.Text;
            var mockResult = (T)(utterancesManager.GetValueOrDefault(text, utterancesManager.NoneIntent) as object);
            return Task.FromResult(mockResult);
        }
    }
}
