using System;
using CalendarSkill.Services;
using Microsoft.Bot.Builder.Dialogs;

namespace CalendarSkill.Prompts.Options
{
    public class GetEventOptions : PromptOptions
    {
        public GetEventOptions(ICalendarService calendarService, TimeZoneInfo timeZone)
            : base()
        {
            CalendarService = calendarService;
            TimeZone = timeZone;
        }

        public ICalendarService CalendarService { get; private set; } = null;

        public TimeZoneInfo TimeZone { get; private set; }
    }
}
