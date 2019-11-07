// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Schema;

namespace Microsoft.Bot.Builder.Solutions.Skills
{
    public class SkillSwitchConfirmOption
    {
        public Activity FallbackHandledEvent { get; set; }

        public string TargetIntent { get; set; }

        public Activity UserInputActivity { get; set; }
    }
}
