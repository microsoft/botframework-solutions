using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CalendarSkill.Models.DialogOptions
{
    public class UpdateTitleDialogOptions
    {
        public UpdateTitleDialogOptions()
        {
            Reason = UpdateReason.ForCreateEvent;
        }

        public UpdateTitleDialogOptions(UpdateReason reason)
        {
            Reason = reason;
        }

        public enum UpdateReason
        {
            /// <summary>
            /// ForCreateEvent.
            /// </summary>
            ForCreateEvent,

            /// <summary>
            /// ForBookMeetingRoom.
            /// </summary>
            ForBookMeetingRoom,
        }

        public UpdateReason Reason { get; set; }
    }
}
