// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using Microsoft.Bot.Builder.Dialogs;

namespace CalendarSkill
{
    public class GreetingDialog : CalendarSkillDialog
    {
        // Constants
        public const string Name = "greetingContainer";

        public GreetingDialog(CalendarSkillServices services, CalendarSkillAccessors accessors, IServiceManager serviceManager)
            : base(Name, services, accessors, serviceManager)
        {
            var greeting = new WaterfallStep[]
            {
                Greeting,
            };

            // Define the conversation flow using a waterfall model.
            AddDialog(new WaterfallDialog(Action.Greeting, greeting));

            // Set starting dialog for component
            InitialDialogId = Action.Greeting;
        }
    }
}
