// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using Microsoft.Bot.Builder.Dialogs;

namespace CalendarSkill
{
    public class CreateEventDialog : CalendarSkillDialog
    {
        // Constants
        public const string Name = "createEventDialog";

        public CreateEventDialog(CalendarSkillServices services, CalendarSkillAccessors accessors, IServiceManager serviceManager)
            : base(Name, services, accessors, serviceManager)
        {
            var createEvent = new WaterfallStep[]
            {
                GetAuthToken,
                AfterGetAuthToken,
                CollectAttendees,
                CollectTitle,
                CollectContent,
                CollectStartDate,
                CollectStartTime,
                CollectDuration,
                CollectLocation,
                ConfirmBeforeCreate,
                CreateEvent,
            };

            var updateAddress = new WaterfallStep[]
            {
                UpdateAddress,
                AfterUpdateAddress,
            };

            var confirmAttendee = new WaterfallStep[]
            {
                ConfirmAttendee,
                AfterConfirmAttendee,
            };

            var updateName = new WaterfallStep[]
            {
                UpdateUserName,
                AfterUpdateUserName,
            };

            var updateStartDate = new WaterfallStep[]
            {
                UpdateStartDateForCreate,
                AfterUpdateStartDateForCreate,
            };

            var updateStartTime = new WaterfallStep[]
            {
                UpdateStartTimeForCreate,
                AfterUpdateStartTimeForCreate,
            };

            var updateDuration = new WaterfallStep[]
            {
                UpdateDurationForCreate,
                AfterUpdateDurationForCreate,
            };

            // Define the conversation flow using a waterfall model.
            AddDialog(new WaterfallDialog(Action.CreateEvent, createEvent));
            AddDialog(new WaterfallDialog(Action.UpdateAddress, updateAddress));
            AddDialog(new WaterfallDialog(Action.ConfirmAttendee, confirmAttendee));
            AddDialog(new WaterfallDialog(Action.UpdateName, updateName));
            AddDialog(new WaterfallDialog(Action.UpdateStartDateForCreate, updateStartDate));
            AddDialog(new WaterfallDialog(Action.UpdateStartTimeForCreate, updateStartTime));
            AddDialog(new WaterfallDialog(Action.UpdateDurationForCreate, updateDuration));

            // Set starting dialog for component
            InitialDialogId = Action.CreateEvent;
        }
    }
}