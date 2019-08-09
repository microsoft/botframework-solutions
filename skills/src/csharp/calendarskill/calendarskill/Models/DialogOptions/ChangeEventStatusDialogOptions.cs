using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CalendarSkill.Models.DialogOptions
{
    public class ChangeEventStatusDialogOptions : CalendarSkillDialogOptions
    {
        public ChangeEventStatusDialogOptions(object options, EventStatus eventStatus)
            : base(options)
        {
            NewEventStatus = eventStatus;
        }

        public EventStatus NewEventStatus { get; set; }
    }
}
