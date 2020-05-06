// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using ITSMSkill.Models;
using Microsoft.Bot.Schema;

namespace ITSMSkill.Services
{
    public class ServiceManager : IServiceManager
    {
        public IITServiceManagement CreateManagement(BotSettings botSettings, TokenResponse tokenResponse, ServiceCache serviceCache)
        {
            if (tokenResponse.ConnectionName == "ServiceNow6" && !string.IsNullOrEmpty(botSettings.ServiceNowUrl) && !string.IsNullOrEmpty(botSettings.ServiceNowGetUserId))
            {
                return new ServiceNow.Management(botSettings.ServiceNowUrl, tokenResponse.Token, botSettings.LimitSize, botSettings.ServiceNowGetUserId, serviceCache);
            }
            else
            {
                return null;
            }
        }
    }
}
