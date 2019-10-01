// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace CalendarSkill.Models.DialogOptions
{
    public class UpdateAddressDialogOptions
    {
        public UpdateAddressDialogOptions()
        {
            Reason = UpdateReason.NotFound;
        }

        public UpdateAddressDialogOptions(UpdateReason reason)
        {
            Reason = reason;
        }

        public enum UpdateReason
        {
            /// <summary>
            /// NotAnAddress.
            /// </summary>
            NotAnAddress,

            /// <summary>
            /// NotFound.
            /// </summary>
            NotFound,
        }

        public UpdateReason Reason { get; set; }
    }
}