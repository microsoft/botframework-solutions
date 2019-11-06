using System;

namespace Microsoft.Bot.Builder.Solutions.Skills
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
