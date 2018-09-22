// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using Microsoft.Bot.Builder.Dialogs;

namespace CalendarSkill
{
    public class UpdateEventDialog : CalendarSkillDialog
    {
        // Constants
        public const string Name = "updateEventDialog";

        public UpdateEventDialog(CalendarSkillServices services, CalendarSkillAccessors accessors, IServiceManager serviceManager)
            : base(Name, services, accessors, serviceManager)
        {
            var updateEvent = new WaterfallStep[]
            {
                GetAuthToken,
                AfterGetAuthToken,
                FromTokenToStartTime,
                FromEventsToNewDate,
                ConfirmBeforeUpdate,
                UpdateEventTime,
            };

            var updateStartTime = new WaterfallStep[]
            {
                UpdateStartTime,
                AfterUpdateStartTime,
            };

            var updateNewStartTime = new WaterfallStep[]
            {
                GetNewEventTime,
                AfterGetNewEventTime,
            };

            // Define the conversation flow using a waterfall model.
            AddDialog(new WaterfallDialog(Action.UpdateEventTime, updateEvent));
            AddDialog(new WaterfallDialog(Action.UpdateStartTime, updateStartTime));
            AddDialog(new WaterfallDialog(Action.UpdateNewStartTime, updateNewStartTime));

            // Set starting dialog for component
            InitialDialogId = Action.UpdateEventTime;
        }
    }
}
