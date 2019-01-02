// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace EmailSkill.Model
{
    /// <summary>
    /// Source of event.
    /// </summary>
    public enum MailSource
    {
        /// <summary>
        /// Event from Microsoft.
        /// </summary>
        Microsoft = 1,

        /// <summary>
        /// Event from Google.
        /// </summary>
        Google = 2,

        /// <summary>
        /// Event from other.
        /// </summary>
        Other = 0,
    }
}