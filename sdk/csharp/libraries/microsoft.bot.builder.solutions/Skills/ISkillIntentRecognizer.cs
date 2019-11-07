// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;

namespace Microsoft.Bot.Builder.Solutions.Skills
{
    public interface ISkillIntentRecognizer
    {
        Func<DialogContext, Task<string>> RecognizeSkillIntent { get; }

        bool ConfirmIntentSwitch { get; }
    }
}
