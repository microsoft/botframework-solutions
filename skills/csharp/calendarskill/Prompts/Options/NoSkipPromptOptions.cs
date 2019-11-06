// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;

namespace CalendarSkill.Options
{
    public class NoSkipPromptOptions : PromptOptions
    {
        public NoSkipPromptOptions()
            : base()
        {
        }

        public Activity NoSkipPrompt { get; set; }
    }
}
