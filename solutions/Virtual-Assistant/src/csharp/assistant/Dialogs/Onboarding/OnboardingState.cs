// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Builder.Dialogs;

namespace VirtualAssistant.Dialogs.Onboarding
{
    public class OnboardingState : DialogState
    {
        public string Name { get; set; }

        public string Location { get; set; }
    }
}