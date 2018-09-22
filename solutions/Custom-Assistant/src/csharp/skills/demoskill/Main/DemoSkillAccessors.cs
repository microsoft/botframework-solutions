// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;

namespace DemoSkill
{
    public class DemoSkillAccessors
    {
        public IStatePropertyAccessor<DemoSkillState> DemoSkillState { get; set; }

        public IStatePropertyAccessor<DialogState> ConversationDialogState { get; set; }
    }
}
