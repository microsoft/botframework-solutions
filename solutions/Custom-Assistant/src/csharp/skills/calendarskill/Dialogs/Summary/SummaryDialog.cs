// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using Microsoft.Bot.Builder.Dialogs;

namespace CalendarSkill
{
    public class SummaryDialog : CalendarSkillDialog
    {
        // Constants
        public const string Name = "showSummaryContainer";

        public SummaryDialog(CalendarSkillServices services, CalendarSkillAccessors accessors, IServiceManager serviceManager)
            : base(Name, services, accessors, serviceManager)
        {
            var showSummary = new WaterfallStep[]
            {
                IfClearContextStep,
                GetAuthToken,
                AfterGetAuthToken,
                ShowEventsSummary,
                PromptToRead,
                CallReadEventDialog,
            };

            var readEvent = new WaterfallStep[]
            {
                ReadEvent,
                AfterReadOutEvent,
            };

            // Define the conversation flow using a waterfall model.
            AddDialog(new WaterfallDialog(Action.ShowEventsSummary, showSummary));
            AddDialog(new WaterfallDialog(Action.Read, readEvent));

            // Set starting dialog for component
            InitialDialogId = Action.ShowEventsSummary;
        }
    }
}
