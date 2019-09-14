// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Bot.Builder.Solutions.Skills
{
    public enum SkillExceptionType
    {
        /// <summary>
        ///  Access Denied when calling external APIs
        /// </summary>
        ApiAccessDenied,

        /// <summary>
        ///  Account Not Activated when calling external APIs
        /// </summary>
        AccountNotActivated,

        /// <summary>
        /// Other types of exceptions
        /// </summary>
        Other,
    }
}
