// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace Microsoft.Bot.Builder.Solutions.Skills
{
    public class SkillException : Exception
    {
        public SkillException()
        {
            ExceptionType = SkillExceptionType.Other;
        }

        public SkillException(string message)
            : base(message)
        {
            ExceptionType = SkillExceptionType.Other;
        }

        public SkillException(string message, Exception innerException)
            : base(message, innerException)
        {
            ExceptionType = SkillExceptionType.Other;
        }

        public SkillException(string message, Exception innerException, SkillExceptionType exceptionType)
            : base(message, innerException)
        {
            ExceptionType = exceptionType;
        }

        public SkillExceptionType ExceptionType { get; set; }
    }
}
