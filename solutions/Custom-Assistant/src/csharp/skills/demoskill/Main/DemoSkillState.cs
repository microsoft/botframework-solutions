// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Builder.Dialogs;

namespace DemoSkill
{
    public class DemoSkillState : DialogState
    {
        public DialogState ConversationDialogState { get; set; }

        public bool SkillMode { get; set; }
    }
}
