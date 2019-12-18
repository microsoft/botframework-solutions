using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CalendarSkill.Models.DialogOptions
{
    public class CollectInfoDialogOptions
    {
        public CollectInfoDialogOptions()
        {
            Reason = UpdateReason.FirstCollect;
        }

        public CollectInfoDialogOptions(UpdateReason reason)
        {
            Reason = reason;
        }

        public enum UpdateReason
        {
            /// <summary>
            /// FirstCollect.
            /// </summary>
            FirstCollect,

            /// <summary>
            /// ReCollect.
            /// </summary>
            ReCollect,
        }

        public UpdateReason Reason { get; set; }
    }
}
