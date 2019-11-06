// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace EmailSkill.Models
{
    public enum ResendEmailState
    {
        /// <summary>
        /// Cancel the recreate
        /// </summary>
        Cancel = 0,

        /// <summary>
        /// Change the Recipients and recerate.
        /// </summary>
        Recipients = 1,

        /// <summary>
        /// Change the subject and recerate.
        /// </summary>
        Subject = 2,

        /// <summary>
        /// Change the content and recerate.
        /// </summary>
        Body = 3
    }
}
