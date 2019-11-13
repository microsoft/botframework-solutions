// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Text;
using Luis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.AI.Luis;
using static Luis.HospitalityLuis;
using static Luis.HospitalityLuis._Entities;

namespace HospitalitySkill.Tests.Flow.Utterances
{
    public class HospitalityTestUtterances : BaseTestUtterances<HospitalityLuis>
    {
        public static readonly string TimexDateFormat = "yyyy-MM-dd";
        public static readonly string TimexTimeFormat = @"'T'hh\:mm\:ss";

        public override HospitalityLuis NoneIntent { get; } = new HospitalityLuis
        {
            Intents = new Dictionary<Intent, IntentScore>
            {
                { Intent.None, new IntentScore() { Score = TopIntentScore } }
            },
            Entities = new _Entities()
        };

        protected void AddIntent(
            string userInput,
            Intent intent,
            string[] hotelNights = null,
            string[] item = null,
            string[] specialRequest = null,
            string[] food = null,
            DateTimeSpec[] datetime = null,
            double[] number = null,
            string[][] menu = null,
            FoodRequestClass[] foodRequest = null,
            ItemRequestClass[] itemRequest = null,
            NumNightsClass[] numNights = null)
        {
            var resultIntent = new HospitalityLuis
            {
                Text = userInput,
                Intents = new Dictionary<Intent, IntentScore>
                {
                    { intent, new IntentScore() { Score = TopIntentScore } }
                }
            };

            resultIntent.Entities = new _Entities
            {
                HotelNights = hotelNights,
                Item = item,
                SpecialRequest = specialRequest,
                Food = food,
                datetime = datetime,
                number = number,
                Menu = menu,
                FoodRequest = foodRequest,
                ItemRequest = itemRequest,
                NumNights = numNights,
            };

            Add(userInput, resultIntent);
        }
    }
}
