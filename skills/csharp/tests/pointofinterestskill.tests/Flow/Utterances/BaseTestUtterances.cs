// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Luis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.AI.Luis;
using PointOfInterestSkill.Tests.Flow.Strings;
using static Luis.PointOfInterestLuis;

namespace PointOfInterestSkill.Tests.Flow.Utterances
{
    public class BaseTestUtterances : Dictionary<string, PointOfInterestLuis>
    {
        public static readonly double LocationLatitude = 47.639620;

        public static readonly double LocationLongitude = -122.130610;

        public static double TopIntentScore { get; } = 0.9;

        public static string LocationEvent { get; } = $"/event:{{ \"Name\": \"Location\", \"Value\": \"{LocationLatitude},{LocationLongitude}\" }}";

        public static string OptionOne { get; } = "option 1";

        public static string OptionTwo { get; } = "option 2";

        public static string OptionThree { get; } = "option 3";

        public static string Yes { get; } = "yes";

        public static string No { get; } = "no";

        public static string Call { get; } = "call";

        public static string ShowDirections { get; } = "show directions";

        public static string StartNavigation { get; } = "start navigation";

        public void AddManager(BaseTestUtterances utterances)
        {
            foreach (var item in utterances)
            {
                if (!this.ContainsKey(item.Key))
                {
                    this.Add(item.Key, item.Value);
                }
            }
        }

        public PointOfInterestLuis GetBaseNoneIntent()
        {
            var result = new PointOfInterestLuis
            {
                Intents = new Dictionary<Intent, IntentScore>()
            };
            result.Intents.Add(Intent.None, new IntentScore() { Score = TopIntentScore });

            return result;
        }

        protected PointOfInterestLuis CreateIntent(
            string userInput,
            Intent intent,
            string[] address = null,
            string[] keyword = null,
            string[][] keywordCategory = null,
            string[][] poiDescription = null,
            string[][] routeDescription = null,
            string[] categoryText = null)
        {
            var poiIntent = new PointOfInterestLuis
            {
                Text = userInput,
                Intents = new Dictionary<Intent, IntentScore>()
            };
            poiIntent.Intents.Add(intent, new IntentScore() { Score = TopIntentScore });

            poiIntent.Entities = new _Entities
            {
                Address = address,
                Keyword = keyword,
                KeywordCategory = keywordCategory,
                PoiDescription = poiDescription,
                RouteDescription = routeDescription,
                _instance = new _Entities._Instance(),
            };

            if (keywordCategory != null)
            {
                poiIntent.Entities._instance.KeywordCategory = categoryText.Select(text => new InstanceData { Text = text }).ToArray();
            }

            return poiIntent;
        }
    }
}
