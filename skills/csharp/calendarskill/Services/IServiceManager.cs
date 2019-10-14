// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using CalendarSkill.Models;
using Microsoft.Bot.Builder;

namespace CalendarSkill.Services
{
    public interface IServiceManager
    {
        IUserService InitUserService(string token, EventSource source, ITurnContext ctx);

        ICalendarService InitCalendarService(string token, EventSource source, ITurnContext ctx);
    }
}