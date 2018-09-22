// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace EmailSkill
{
    /// <summary>
    /// Update user dialog options.
    /// </summary>
    public class UpdateUserDialogOptions
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UpdateUserDialogOptions"/> class.
        /// </summary>
        public UpdateUserDialogOptions()
        {
            this.Reason = UpdateReason.TooMany;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UpdateUserDialogOptions"/> class.
        /// </summary>
        /// <param name="reason">The reason of update user name.</param>
        public UpdateUserDialogOptions(UpdateReason reason)
        {
            this.Reason = reason;
        }

        /// <summary>
        /// The reason of update user's name in flow.
        /// </summary>
        public enum UpdateReason
        {
            /// <summary>
            /// Too many people with the same name.
            /// </summary>
            TooMany,

            /// <summary>
            /// The person not found.
            /// </summary>
            NotFound,
        }

        /// <summary>
        /// Gets or sets the reason of update user.
        /// </summary>
        /// <value>
        /// The reason of update user.
        /// </value>
        public UpdateReason Reason { get; set; }
    }
}
