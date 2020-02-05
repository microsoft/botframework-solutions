// https://docs.microsoft.com/en-us/visualstudio/modeling/t4-include-directive?view=vs-2017
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Solutions.Responses;

namespace HospitalitySkill.Responses.RoomService
{
    /// <summary>
    /// Contains bot responses.
    /// </summary>
    public class RoomServiceResponses : IResponseIdCollection
    {
        // Generated accessors
        public const string MenuPrompt = "MenuPrompt";
        public const string ChooseOneMenu = "ChooseOneMenu";
        public const string FoodOrder = "FoodOrder";
        public const string RetryFoodOrder = "RetryFoodOrder";
        public const string ItemsNotAvailable = "ItemsNotAvailable";
        public const string AddMore = "AddMore";
        public const string ConfirmOrder = "ConfirmOrder";
        public const string FinalOrderConfirmation = "FinalOrderConfirmation";
    }
}