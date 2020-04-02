namespace ITSMSkill.Utilities
{
    using System;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;

    #region class Ensure
    /// <summary>
    /// Contract checkers. Each checker verifies some Boolean condition,
    /// and if the condition is not met (meaning it returns false),
    /// the following actions are taken:
    /// 
    /// 1. A trace is written.
    /// 2. [In Debug builds only:] Debug.Fail is invoked.
    /// 3. An exception is raised.
    /// 
    /// This class has no dependencies outside BCL and tracing.
    /// </summary>
    public static class Ensure
    {
        /// <summary>
        /// A dummy method that appeases the compiler's warning about unused values
        /// </summary>
        public static T ValueIsUsed<T>(T value)
        {
            return value;
        }

        /// <summary>
        /// A dummy method that appeases the compiler's warning about unused values
        /// </summary>
        public static T[] ValuesAreUsed<T>(params T[] values)
        {
            return values;
        }

        /// <summary>
        /// Throws an <see cref="ArgumentNullException"/> or an <see cref="ArgumentException"/>
        /// depending on whether <paramref name="value"/> is null or empty.
        /// </summary>
        public static string ArgIsNotNullOrEmpty(
            string value,
            string argName,
            [CallerMemberName] string callerMemberName = "",
            [CallerFilePath] string callerFilePath = "",
            [CallerLineNumber] int callerLineNumber = 0)
        {
            if (string.IsNullOrEmpty(value))
            {
                string what = (value == null) ? "null" : "empty";
                var detailedMessage = FormatErrorMessage("Argument '" + argName + "' is " + what,
                    null, callerMemberName, callerFilePath, callerLineNumber);

                Debug.Fail(detailedMessage);
                throw (value == null)
                    ? new ArgumentNullException(argName, detailedMessage)
                    : new ArgumentException(argName, detailedMessage);
            }

            return value;
        }

        /// <summary>
        /// Throws an <see cref="ArgumentNullException"/> or an <see cref="ArgumentException"/>
        /// depending on whether <paramref name="value"/> is null or empty/whitespace.
        /// </summary>
        public static string ArgIsNotNullOrWhiteSpace(
            string value,
            string argName,
            [CallerMemberName] string callerMemberName = "",
            [CallerFilePath] string callerFilePath = "",
            [CallerLineNumber] int callerLineNumber = 0)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                string what = (value == null)
                    ? "null"
                    : (string.IsNullOrEmpty(value) ? "empty" : "whitespace");
                var detailedMessage = FormatErrorMessage("Argument '" + argName + "' is " + what,
                    null, callerMemberName, callerFilePath, callerLineNumber);

                Debug.Fail(detailedMessage);
                throw (value == null)
                    ? new ArgumentNullException(argName, detailedMessage)
                    : new ArgumentException(argName, detailedMessage);
            }

            return value;
        }

        /// <summary>
        /// Throws an <see cref="ArgumentNullException"/> if the argument is null.
        /// </summary>
        public static T ArgIsNotNull<T>(
            T value,
            string argName,
            [CallerMemberName] string callerMemberName = "",
            [CallerFilePath] string callerFilePath = "",
            [CallerLineNumber] int callerLineNumber = 0)
        {
            if (value != null)
            {
                return value;
            }

            var detailedMessage = FormatErrorMessage("Argument '" + argName + "' is null",
                null, callerMemberName, callerFilePath, callerLineNumber);

            throw new ArgumentNullException(argName, detailedMessage);
        }

        private static string FormatErrorMessage(
            string message,
            Exception ex,
            string callerMemberName,
            string callerFilePath,
            int callerLineNumber)
        {
            return string.Format(
                "{0}: at {1} in {2}: line {3}{4}",
                string.IsNullOrWhiteSpace(message) ? (object)"[No message specified]" : (object)message,
                string.IsNullOrWhiteSpace(callerMemberName) ? (object)"?" : (object)callerMemberName,
                string.IsNullOrWhiteSpace(callerFilePath) ? (object)"?" : (object)callerFilePath,
                (object)callerLineNumber,
                ex);
        }
    }

    #endregion

}
