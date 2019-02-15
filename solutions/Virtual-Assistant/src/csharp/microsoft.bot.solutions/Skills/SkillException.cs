using System;

namespace Microsoft.Bot.Solutions.Skills
{
    public enum SkillExceptionType
    {
        /// <summary>
        ///  Access Denied when calling external APIs
        /// </summary>
        APIAccessDenied,

        /// <summary>
        /// Other types of exceptions
        /// </summary>
        Other
    }

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