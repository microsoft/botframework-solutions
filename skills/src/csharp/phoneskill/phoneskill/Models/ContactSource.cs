// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace PhoneSkill.Models
{
    /// <summary>
    /// Where the skill gets the user's contacts from.
    /// </summary>
    public enum ContactSource
    {
        /// <summary>
        /// Microsoft Graph API.
        /// </summary>
        Microsoft,

        /// <summary>
        /// Google People API.
        /// </summary>
        Google,
    }
}
