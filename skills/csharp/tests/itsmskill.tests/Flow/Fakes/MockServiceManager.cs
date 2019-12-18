// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using ITSMSkill.Models;
using ITSMSkill.Services;
using ITSMSkill.Services.ServiceNow;
using ITSMSkill.Tests.API.Fakes;
using Microsoft.Bot.Schema;

namespace ITSMSkill.Tests.Flow.Fakes
{
    public class MockServiceManager : IServiceManager
    {
        public IITServiceManagement CreateManagement(BotSettings botSettings, TokenResponse tokenResponse, ServiceCache serviceCache)
        {
            // TODO check tokenResponse.ConnectionName == "ServiceNow"
            if (!string.IsNullOrEmpty(botSettings.ServiceNowUrl) && !string.IsNullOrEmpty(botSettings.ServiceNowGetUserId))
            {
                return new Management(botSettings.ServiceNowUrl, tokenResponse.Token, botSettings.LimitSize, botSettings.ServiceNowGetUserId, serviceCache, new MockServiceNowRestClient().MockRestClient);
            }
            else
            {
                return null;
            }
        }
    }
}
