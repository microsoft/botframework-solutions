// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Bot.Solutions.Skills
{
    public enum SkillExceptionType
    {
        /// <summary>
        ///  Access Denied when calling external APIs
        /// </summary>
        APIAccessDenied,

        /// <summary>
        ///  Account Not Activated when calling external APIs
        /// </summary>
        AccountNotActivated,

        /// <summary>
        ///  Bad Request returned when calling external APIs
        /// </summary>
        APIBadRequest,

        /// <summary>
        ///  Unauthorized returned when calling external APIs
        /// </summary>
        APIUnauthorized,

        /// <summary>
        ///  Forbidden returned when calling external APIs
        /// </summary>
        APIForbidden,

        /// <summary>
        /// Other types of exceptions
        /// </summary>
        Other,
    }

    [ExcludeFromCodeCoverageAttribute]
    public class SkillException : Exception
    {
        public SkillException(SkillExceptionType exceptionType, string message, Exception innerException)
            : base(message, innerException)
        {
            ExceptionType = exceptionType;
        }

        public SkillExceptionType ExceptionType { get; set; }
    }
}