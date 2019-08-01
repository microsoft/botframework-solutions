using Microsoft.Bot.Builder.Solutions.Responses;
using System;

namespace CalendarSkill.Models
{
    public class CalendarDateCardData : ICardData
    {
        public DateTime Date { get; set; }
    }
}