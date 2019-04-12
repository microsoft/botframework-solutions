using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using Microsoft.Bot.Builder.Skills.Auth;
using Microsoft.Bot.Builder.Skills.Models.Manifest;
using Microsoft.Bot.Builder.Solutions.Responses;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RichardSzalay.MockHttp;

namespace Microsoft.Bot.Builder.Skills.Tests
{
    // Extended implementation of SkillDialog for test purposes that enables us to mock the HttpClient
    internal class SkillDialogTest : SkillDialog
    {
        private MockHttpMessageHandler _mockHttpMessageHandler;
        public SkillDialogTest(SkillManifest skillManifest, Models.Manifest.Action action, ResponseManager responseManager, MicrosoftAppCredentialsEx microsoftAppCredentialsEx, IBotTelemetryClient telemetryClient, MockHttpMessageHandler mockHttpMessageHandler, UserState userState) : base(skillManifest, action, responseManager, microsoftAppCredentialsEx, telemetryClient, userState)
        {
            _mockHttpMessageHandler = mockHttpMessageHandler;
            _httpClient = mockHttpMessageHandler.ToHttpClient();
        }
    }
}
