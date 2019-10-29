using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CalendarSkill.Models.DialogOptions
{
    public class RouteSearchDialogOptions
    {
        public RouteSearchDialogOptions()
        {
            Reason = UpdateReason.ByDefault;
        }

        public RouteSearchDialogOptions(UpdateReason reason)
        {
            Reason = reason;
        }

        public enum UpdateReason
        {
            /// <summary>
            /// ByDefault.
            /// </summary>
            ByDefault,

            /// <summary>
            /// ByName.
            /// </summary>
            ByName,
        }

        public UpdateReason Reason { get; set; }
    }
}
