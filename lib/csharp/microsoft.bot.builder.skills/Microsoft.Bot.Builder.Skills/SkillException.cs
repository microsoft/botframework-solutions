using System;

namespace Microsoft.Bot.Builder.Skills
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