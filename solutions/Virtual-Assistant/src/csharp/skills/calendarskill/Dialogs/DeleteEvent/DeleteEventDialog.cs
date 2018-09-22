// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using Microsoft.Bot.Builder.Dialogs;

namespace CalendarSkill
{
    public class DeleteEventDialog : CalendarSkillDialog
    {
        // Constants
        public const string Name = "deleteEventDialog";

        public DeleteEventDialog(CalendarSkillServices services, CalendarSkillAccessors accessors, IServiceManager serviceManager)
           : base(Name, services, accessors, serviceManager)
        {
            var deleteEvent = new WaterfallStep[]
            {
                GetAuthToken,
                AfterGetAuthToken,
                FromTokenToStartTime,
                ConfirmBeforeDelete,
                DeleteEventByStartTime,
            };

            var updateStartTime = new WaterfallStep[]
            {
                UpdateStartTime,
                AfterUpdateStartTime,
            };

            AddDialog(new WaterfallDialog(Action.DeleteEvent, deleteEvent));
            AddDialog(new WaterfallDialog(Action.UpdateStartTime, updateStartTime));

            // Set starting dialog for component
            InitialDialogId = Action.DeleteEvent;
        }
    }
}
