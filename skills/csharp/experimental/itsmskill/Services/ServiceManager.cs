// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Schema;

namespace ITSMSkill.Services
{
    public class ServiceManager : IServiceManager
    {
        public IITServiceManagement CreateManagement(BotSettings botSettings, TokenResponse tokenResponse)
        {
            if (tokenResponse.ConnectionName == "ServiceNow" && !string.IsNullOrEmpty(botSettings.ServiceNowUrl) && !string.IsNullOrEmpty(botSettings.ServiceNowGetUserId))
            {
                return new ServiceNow.Management(botSettings.ServiceNowUrl, tokenResponse.Token, botSettings.LimitSize, botSettings.ServiceNowGetUserId);
            }
            else
            {
                return null;
            }
        }
    }
}
