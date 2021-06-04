// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Solutions;
using VirtualAssistantSample.TokenExchange;

namespace VirtualAssistantSample.Services
{
    public class BotSettings : BotSettingsBase
    {
        public TokenExchangeConfig TokenExchangeConfig { get; set; }
        public bool LogPersonalData { get; set; }
    }
}