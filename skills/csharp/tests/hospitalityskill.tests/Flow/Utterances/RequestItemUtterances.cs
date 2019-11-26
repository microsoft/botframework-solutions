// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using static Luis.HospitalityLuis;

namespace HospitalitySkill.Tests.Flow.Utterances
{
    public class RequestItemUtterances : HospitalityTestUtterances
    {
        public static readonly string Item = "Towel";

        public static readonly string InvalidItem = "TV";

        public static readonly string RequestItem = "i need something for my room";

        public static readonly string RequestWithItemAndInvalidItem = $"do you have {Item} and {InvalidItem}";

        public RequestItemUtterances()
        {
            AddIntent(RequestItem, Intent.RequestItem);
            AddIntent(RequestWithItemAndInvalidItem, Intent.RequestItem, item: new string[] { Item, InvalidItem });
        }
    }
}
