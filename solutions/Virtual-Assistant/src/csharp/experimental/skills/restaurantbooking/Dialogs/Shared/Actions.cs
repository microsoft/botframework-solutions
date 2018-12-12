namespace RestaurantBooking.Dialogs.Shared
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    public class Actions
    {
        public const string BookRestaurant = "bookRestaurant";
        public const string AskForFoodType = "askForFoodType";
        public const string AskReserveForExistingMeetingStep = "askReserveForExistingMeetingStep";
        public const string AskReservationDateStep = "askReservationDateStep";
        public const string AskReservationTimeStep = "askReservationTimeStep";
        public const string AskAttendeeCountStep = "askAttendeeCountStep";
        public const string ConfirmSelectionBeforeBookingStep = "confirmSelectionBeforeBookingStep";
        public const string RestaurantPrompt = "restaurantPrompt";
    }
}
