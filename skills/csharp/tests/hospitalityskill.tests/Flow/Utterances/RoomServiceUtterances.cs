// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using static Luis.HospitalityLuis;
using static Luis.HospitalityLuis._Entities;

namespace HospitalitySkill.Tests.Flow.Utterances
{
    public class RoomServiceUtterances : HospitalityTestUtterances
    {
        public static readonly string Breakfast = "breakfast";

        public static readonly string Coffee = "coffee";

        public static readonly string RoomService = "i need some food";

        public static readonly string RoomServiceWithMenu = $"can i see a {Breakfast} menu";

        public static readonly string RoomServiceWithFood = $"i need a {Coffee}";

        public RoomServiceUtterances()
        {
            AddIntent(Breakfast, Intent.None, menu: new string[][] { new string[] { Breakfast } });

            AddIntent(RoomService, Intent.RoomService);
            AddIntent(RoomServiceWithMenu, Intent.RoomService, menu: new string[][] { new string[] { Breakfast } });
            AddIntent(RoomServiceWithFood, Intent.RoomService, food: new string[] { Coffee });
        }
    }
}
