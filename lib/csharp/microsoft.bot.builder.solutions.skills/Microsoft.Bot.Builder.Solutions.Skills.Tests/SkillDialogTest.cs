// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Builder.Skills;
using Microsoft.Bot.Builder.Solutions.Skills.Models;

namespace Microsoft.Bot.Builder.Solutions.Skills.Tests
{
    // Extended implementation of SkillDialog for test purposes that enables us to mock the HttpClient
    internal class SkillDialogTest : SkillDialog
    {
        public SkillDialogTest(SkillConnectionConfiguration skillConnectionConfiguration, ISkillProtocolHandler skillProtocolHandler, IBotTelemetryClient telemetryClient)
            : base(skillConnectionConfiguration, skillProtocolHandler, telemetryClient)
        {
        }
    }
}
