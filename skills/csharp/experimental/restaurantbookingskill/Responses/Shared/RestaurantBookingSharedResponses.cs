// https://docs.microsoft.com/en-us/visualstudio/modeling/t4-include-directive?view=vs-2017
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Solutions.Responses;

namespace RestaurantBookingSkill.Responses.Shared
{
    /// <summary>
    /// Contains bot responses.
    /// </summary>
    public class RestaurantBookingSharedResponses : IResponseIdCollection
    {
        // Generated accessors
        public const string DidntUnderstandMessage = "DidntUnderstandMessage";
        public const string DidntUnderstandMessageIgnoringInput = "DidntUnderstandMessageIgnoringInput";
        public const string CancellingMessage = "CancellingMessage";
        public const string NoAuth = "NoAuth";
        public const string AuthFailed = "AuthFailed";
        public const string ActionEnded = "ActionEnded";
        public const string ErrorMessage = "ErrorMessage";
        public const string BookRestaurantFlowStartMessage = "BookRestaurantFlowStartMessage";
        public const string BookRestaurantFlowStartMessageTextAndSpeak = "BookRestaurantFlowStartMessageTextAndSpeak";
        public const string BookRestaurantFlowStartMessageTextAndSpeak1 = "BookRestaurantFlowStartMessageTextAndSpeak1";
        public const string BookRestaurantFoodSelectionPrompt = "BookRestaurantFoodSelectionPrompt";
        public const string BookRestaurantFoodSelectionPromptTextAndSpeak = "BookRestaurantFoodSelectionPromptTextAndSpeak";
        public const string BookRestaurantFoodSelectionPromptTextAndSpeak1 = "BookRestaurantFoodSelectionPromptTextAndSpeak1";
        public const string BookRestaurantFoodSelectionEcho = "BookRestaurantFoodSelectionEcho";
        public const string BookRestaurantAttendeePrompt = "BookRestaurantAttendeePrompt";
        public const string BookRestaurantReservationMeetingInfoPrompt = "BookRestaurantReservationMeetingInfoPrompt";
        public const string BookRestaurantReservationMeetingInfoPromptTextAndSpeak = "BookRestaurantReservationMeetingInfoPromptTextAndSpeak";
        public const string BookRestaurantReservationMeetingInfoPromptTextAndSpeak1 = "BookRestaurantReservationMeetingInfoPromptTextAndSpeak1";
        public const string BookRestaurantDatePrompt = "BookRestaurantDatePrompt";
        public const string BookRestaurantTimePrompt = "BookRestaurantTimePrompt";
        public const string BookRestaurantDateTimeEcho = "BookRestaurantDateTimeEcho";
        public const string BookRestaurantConfirmationPrompt = "BookRestaurantConfirmationPrompt";
        public const string BookRestaurantAcceptedMessage = "BookRestaurantAcceptedMessage";
        public const string BookRestaurantRestaurantSearching = "BookRestaurantRestaurantSearching";
        public const string BookRestaurantRestaurantSelectionPrompt = "BookRestaurantRestaurantSelectionPrompt";
        public const string BookRestaurantRestaurantSelectionPromptTextAndSpeak = "BookRestaurantRestaurantSelectionPromptTextAndSpeak";
        public const string BookRestaurantRestaurantSelectionPromptTextAndSpeak1 = "BookRestaurantRestaurantSelectionPromptTextAndSpeak1";
        public const string BookRestaurantBookingPlaceSelectionEcho = "BookRestaurantBookingPlaceSelectionEcho";
        public const string FoodTypeSelectionErrorMessage = "FoodTypeSelectionErrorMessage";
        public const string FoodTypeSelectionErrorMessageTextAndSpeak = "FoodTypeSelectionErrorMessageTextAndSpeak";
        public const string FoodTypeSelectionErrorMessageTextAndSpeak1 = "FoodTypeSelectionErrorMessageTextAndSpeak1";
        public const string BookRestaurantRestaurantNegativeConfirm = "BookRestaurantRestaurantNegativeConfirm";
        public const string AmbiguousTimePrompt = "AmbiguousTimePrompt";
    }
}

