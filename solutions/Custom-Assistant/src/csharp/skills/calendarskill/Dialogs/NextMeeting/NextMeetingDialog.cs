// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using Microsoft.Bot.Builder.Dialogs;

namespace CalendarSkill
{
    public class NextMeetingDialog : CalendarSkillDialog
    {
        // Constants
        public const string Name = "showNextEventContainer";

        public NextMeetingDialog(CalendarSkillServices services, CalendarSkillAccessors accessors, IServiceManager serviceManager)
            : base(Name, services, accessors, serviceManager)
        {
            var nextMeeting = new WaterfallStep[]
            {
                GetAuthToken,
                AfterGetAuthToken,
                ShowNextEvent,
            };

            // Define the conversation flow using a waterfall model.
            AddDialog(new WaterfallDialog(Action.ShowEventsSummary, nextMeeting));

            // Set starting dialog for component
            InitialDialogId = Action.ShowEventsSummary;
        }
    }
}
