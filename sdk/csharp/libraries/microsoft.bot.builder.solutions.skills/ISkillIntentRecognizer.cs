// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;

namespace Microsoft.Bot.Builder.Skills
{
    public interface ISkillIntentRecognizer
    {
        Func<DialogContext, Task<string>> RecognizeSkillIntentAsync { get; }

        bool ConfirmIntentSwitch { get; }
    }
}
