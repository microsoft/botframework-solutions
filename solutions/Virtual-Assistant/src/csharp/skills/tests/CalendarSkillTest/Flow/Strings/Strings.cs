using System;
using System.Collections.Generic;
using System.Text;

namespace CalendarSkillTest.Flow.Strings
{
    public class Strings
    {
        public static string DefaultUserName { get; } = "test name";

        public static string DefaultUserEmail { get; } = "test@test.com";

        public static string DefaultEventName { get; } = "test title";

        public static string DefaultContent { get; } = "test content";

        public static string DefaultLocation { get; } = "test location";

        public static string DefaultStartDate { get; } = "tomorrow";

        public static string DefaultStartTime { get; } = "9 AM";

        public static string DefaultEndTime { get; } = "10 AM";

        public static string DefaultDuration { get; } = "one hour";

        public static string ConfirmYes { get; } = "yes";

        public static string ConfirmNo { get; } = "no";

        public static string WeekdayDate { get; } = "Friday";

        public static string FirstOne { get; } = "first one";

        public static string UserName { get; } = "test name {0}";

        public static string UserEmailAddress { get; } = "test{0}@test.com";

        public static string Next { get; } = "next";

        public static string ThrowErrorAccessDenied { get; } = "test_throw_error_access_denied";
    }
}
