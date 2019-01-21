using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CalendarSkill.Models
{
    public class CreateEventStateModel
    {
        public enum RecreateEventState
        {
            /// <summary>
            /// Cancel the recreate
            /// </summary>
            Cancel = 0,

            /// <summary>
            /// Change the time and recerate.
            /// </summary>
            Time = 1,

            /// <summary>
            /// Change the duration and recerate.
            /// </summary>
            Duration = 2,

            /// <summary>
            /// Change the location and recerate.
            /// </summary>
            Location = 3,

            /// <summary>
            /// Change the participants and recerate.
            /// </summary>
            Participants = 4,

            /// <summary>
            /// Change the subject and recerate.
            /// </summary>
            Subject = 5,

            /// <summary>
            /// Change the content and recerate.
            /// </summary>
            Content = 6
        }
    }
}
