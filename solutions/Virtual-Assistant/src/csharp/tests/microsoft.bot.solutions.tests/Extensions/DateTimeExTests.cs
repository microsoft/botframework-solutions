using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.Bot.Solutions.Extensions;
using Microsoft.Bot.Solutions.Resources;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Bot.Solutions.Tests.Extensions
{
    [TestClass]
    public class DateTimeExTests
    {
        private CultureInfo _currentUICulture = CultureInfo.CurrentUICulture;

        [TestInitialize]
        public void Init()
        {
            _currentUICulture = CultureInfo.CurrentUICulture;
        }

        [TestCleanup]
        public void CleanUp()
        {
            CultureInfo.CurrentUICulture = _currentUICulture;
        }

        [TestMethod]
        public void TestUsingExplicitValue()
        {
            // Setup test data
            var today = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 17, 00, 00);
            var tomorrow = today.AddDays(1);
            var specificDate = new DateTime(1975, 04, 04, 1, 20, 42);
            var specificDatePluralHour = new DateTime(1975, 04, 04, 4, 30, 42);
            var testData = new List<DateTimeTestData>()
            {
                // US English
                new DateTimeTestData("en-US"){ InputDateTime = today,
                    ExpectedDateSpeech = "Today", ExpectedDateSpeechWithSuffix = "Today",
                    ExpectedTimeSpeech = string.Format($"{today:h:mm tt}"), ExpectedTimeSpeechWithSuffix = string.Format($"at {today:h:mm tt}")},

                new DateTimeTestData("en-US"){ InputDateTime = tomorrow,
                    ExpectedDateSpeech = "Tomorrow", ExpectedDateSpeechWithSuffix = "Tomorrow",
                    ExpectedTimeSpeech = string.Format($"{tomorrow:h:mm tt}"), ExpectedTimeSpeechWithSuffix = string.Format($"at {tomorrow:h:mm tt}")},

                new DateTimeTestData("en-US"){ InputDateTime = specificDate,
                    ExpectedDateSpeech = "Friday, April 04", ExpectedDateSpeechWithSuffix = "Friday, April 04",
                    ExpectedTimeSpeech = string.Format($"{specificDate:h:mm tt}"), ExpectedTimeSpeechWithSuffix = string.Format($"at {specificDate:h:mm tt}")},

                new DateTimeTestData("en-US"){ InputDateTime = specificDatePluralHour,
                    ExpectedDateSpeech = "Friday, April 04", ExpectedDateSpeechWithSuffix = "Friday, April 04",
                    ExpectedTimeSpeech = string.Format($"{specificDatePluralHour:h:mm tt}"), ExpectedTimeSpeechWithSuffix = string.Format($"at {specificDatePluralHour:h:mm tt}")},

                // Spanish from Spain (uses 24 hr format)
                new DateTimeTestData("es-ES"){ InputDateTime = today,
                    ExpectedDateSpeech = "hoy", ExpectedDateSpeechWithSuffix = "hoy",
                    ExpectedTimeSpeech = string.Format($"{today:H:mm}"), ExpectedTimeSpeechWithSuffix = string.Format($"a las {today:H:mm}")},

                new DateTimeTestData("es-ES"){ InputDateTime = tomorrow,
                    ExpectedDateSpeech = "mañana", ExpectedDateSpeechWithSuffix = "mañana",
                    ExpectedTimeSpeech = string.Format($"{tomorrow:H:mm}"), ExpectedTimeSpeechWithSuffix = string.Format($"a las {tomorrow:H:mm}")},

                new DateTimeTestData("es-ES"){ InputDateTime = specificDate,
                    ExpectedDateSpeech = "viernes 04 de abril", ExpectedDateSpeechWithSuffix = "el viernes 04 de abril",
                    ExpectedTimeSpeech = string.Format($"{specificDate:H:mm}"), ExpectedTimeSpeechWithSuffix = string.Format($"a la {specificDate:H:mm}")},

                new DateTimeTestData("es-ES"){ InputDateTime = specificDatePluralHour,
                    ExpectedDateSpeech = "viernes 04 de abril", ExpectedDateSpeechWithSuffix = "el viernes 04 de abril",
                    ExpectedTimeSpeech = string.Format($"{specificDatePluralHour:H:mm}"), ExpectedTimeSpeechWithSuffix = string.Format($"a las {specificDatePluralHour:H:mm}")},


                // Spanish from Mexico (uses AM PM)
                new DateTimeTestData("es-MX"){ InputDateTime = specificDate,
                    ExpectedDateSpeech = "viernes 04 de abril", ExpectedDateSpeechWithSuffix = "el viernes 04 de abril",
                    ExpectedTimeSpeech = string.Format($"{specificDate:hh:mm tt}"), ExpectedTimeSpeechWithSuffix = string.Format($"a la {specificDate:hh:mm tt}")},

                new DateTimeTestData("es-MX"){ InputDateTime = specificDatePluralHour,
                    ExpectedDateSpeech = "viernes 04 de abril", ExpectedDateSpeechWithSuffix = "el viernes 04 de abril",
                    ExpectedTimeSpeech = string.Format($"{specificDatePluralHour:hh:mm tt}"), ExpectedTimeSpeechWithSuffix = string.Format($"a las {specificDatePluralHour:hh:mm tt}")},

            };

            foreach (var data in testData)
            {
                CultureInfo.CurrentUICulture = data.Culture;
                Assert.AreEqual(data.ExpectedDateSpeech, data.InputDateTime.ToSpeechDateString());
                Assert.AreEqual(data.ExpectedDateSpeechWithSuffix, data.InputDateTime.ToSpeechDateString(true));
                Assert.AreEqual(data.ExpectedTimeSpeech, data.InputDateTime.ToSpeechTimeString());
                Assert.AreEqual(data.ExpectedTimeSpeechWithSuffix, data.InputDateTime.ToSpeechTimeString(true));
            }
        }

        [TestMethod]
        public void TestUsingResourceValues()
        {
            var cultures = new[] {"en-US", "es-ES", "es-MX", "de-DE", "it", "zh", "fr" };
            foreach (var culture in cultures)
            {
                CultureInfo.CurrentUICulture = new CultureInfo(culture);
                Assert.AreEqual(CommonStrings.Today, DateTime.UtcNow.ToSpeechDateString());
                Assert.AreEqual(CommonStrings.Tomorrow, DateTime.UtcNow.AddDays(1).ToSpeechDateString());
                Assert.AreEqual(DateTime.UtcNow.AddDays(3).ToString(CommonStrings.SpokenDateFormat, CultureInfo.CurrentUICulture), DateTime.UtcNow.AddDays(3).ToSpeechDateString());
                if (string.IsNullOrEmpty(CommonStrings.SpokenDatePrefix))
                {
                    Assert.AreEqual($"{DateTime.UtcNow.AddDays(3).ToString(CommonStrings.SpokenDateFormat, CultureInfo.CurrentUICulture)}", DateTime.UtcNow.AddDays(3).ToSpeechDateString(true));
                }
                else
                {
                    Assert.AreEqual($"{CommonStrings.SpokenDatePrefix} {DateTime.UtcNow.AddDays(3).ToString(CommonStrings.SpokenDateFormat, CultureInfo.CurrentUICulture)}", DateTime.UtcNow.AddDays(3).ToSpeechDateString(true));
                }
            }
        }

        private class DateTimeTestData
        {
            public DateTimeTestData(string culture)
            {
                Culture = new CultureInfo(culture);
            }
            public CultureInfo Culture { get; }
            public DateTime InputDateTime { get; set; }
            public string ExpectedDateSpeech { get; set; }
            public string ExpectedDateSpeechWithSuffix { get; set; }
            public string ExpectedTimeSpeech { get; set; }
            public string ExpectedTimeSpeechWithSuffix { get; set; }
        }
    }
}