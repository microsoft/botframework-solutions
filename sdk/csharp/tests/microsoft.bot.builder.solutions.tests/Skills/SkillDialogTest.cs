// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Builder.Solutions.Skills;
using Microsoft.Bot.Builder.Solutions.Skills.Auth;
using Microsoft.Bot.Builder.Solutions.Skills.Models.Manifest;

namespace Microsoft.Bot.Builder.Solutions.Tests.Skills
{
    // Extended implementation of SkillDialog for test purposes that enables us to mock the HttpClient
    internal class SkillDialogTest : SkillDialog
    {
        public SkillDialogTest(SkillManifest skillManifest, IServiceClientCredentials serviceClientCredentials, IBotTelemetryClient telemetryClient, UserState userState, ISkillTransport skillTransport = null)
            : base(skillManifest, serviceClientCredentials, telemetryClient, userState, null, null, skillTransport)
        {
        }
    }
}