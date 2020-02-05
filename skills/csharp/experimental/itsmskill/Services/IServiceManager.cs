// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using ITSMSkill.Models;
using Microsoft.Bot.Schema;

namespace ITSMSkill.Services
{
    public interface IServiceManager
    {
        IITServiceManagement CreateManagement(BotSettings botSettings, TokenResponse tokenResponse, ServiceCache serviceCache = null);
    }
}
