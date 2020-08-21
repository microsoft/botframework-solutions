// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Microsoft.Bot.Solutions.Extensions;
using Microsoft.Bot.Solutions.Resources;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Bot.Solutions.Tests.Extensions
{
    [TestClass]
    [TestCategory("UnitTests")]
    [ExcludeFromCodeCoverageAttribute]
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
            var englishUsCulture = "en-US";
            var englishUsPattern = new CultureInfo(englishUsCulture).DateTimeFormat.ShortTimePattern;
            var spanishSpainCulture = "es-ES";
            var spanishSpainPattern = new CultureInfo(spanishSpainCulture).DateTimeFormat.ShortTimePattern;
            var spanishMexicoCulture = "es-MX";
            var spanishMexicoPattern = new CultureInfo(spanishMexicoCulture).DateTimeFormat.ShortTimePattern;
            var testData = new List<DateTimeTestData>()
            {
                // US English
                new DateTimeTestData(englishUsCulture)
                {
                    InputDateTime = today,
                    ExpectedDateSpeech = "Today", ExpectedDateSpeechWithSuffix = "Today",
                    ExpectedTimeSpeech = today.ToString(englishUsPattern), ExpectedTimeSpeechWithSuffix = $"at {today.ToString(englishUsPattern)}",
                },

                new DateTimeTestData(englishUsCulture)
                {
                    InputDateTime = tomorrow,
                    ExpectedDateSpeech = "Tomorrow", ExpectedDateSpeechWithSuffix = "Tomorrow",
                    ExpectedTimeSpeech = tomorrow.ToString(englishUsPattern), ExpectedTimeSpeechWithSuffix = $"at {tomorrow.ToString(englishUsPattern)}",
                },

                new DateTimeTestData(englishUsCulture)
                {
                    InputDateTime = specificDate,
                    ExpectedDateSpeech = "Friday, April 04", ExpectedDateSpeechWithSuffix = "Friday, April 04",
                    ExpectedTimeSpeech = specificDate.ToString(englishUsPattern), ExpectedTimeSpeechWithSuffix = $"at {specificDate.ToString(englishUsPattern)}",
                },

                new DateTimeTestData(englishUsCulture)
                {
                    InputDateTime = specificDatePluralHour,
                    ExpectedDateSpeech = "Friday, April 04", ExpectedDateSpeechWithSuffix = "Friday, April 04",
                    ExpectedTimeSpeech = specificDatePluralHour.ToString(englishUsPattern), ExpectedTimeSpeechWithSuffix = $"at {specificDatePluralHour.ToString(englishUsPattern)}",
                },

                // Spanish from Spain (uses 24 hr format)
                new DateTimeTestData(spanishSpainCulture)
                {
                    InputDateTime = today,
                    ExpectedDateSpeech = "hoy", ExpectedDateSpeechWithSuffix = "hoy",
                    ExpectedTimeSpeech = today.ToString(spanishSpainPattern), ExpectedTimeSpeechWithSuffix = $"a las {today.ToString(spanishSpainPattern)}",
                },

                new DateTimeTestData(spanishSpainCulture)
                {
                    InputDateTime = tomorrow,
                    ExpectedDateSpeech = "mañana", ExpectedDateSpeechWithSuffix = "mañana",
                    ExpectedTimeSpeech = tomorrow.ToString(spanishSpainPattern), ExpectedTimeSpeechWithSuffix = $"a las {tomorrow.ToString(spanishSpainPattern)}",
                },

                new DateTimeTestData(spanishSpainCulture)
                {
                    InputDateTime = specificDate,
                    ExpectedDateSpeech = "viernes 04 de abril", ExpectedDateSpeechWithSuffix = "el viernes 04 de abril",
                    ExpectedTimeSpeech = specificDate.ToString(spanishSpainPattern), ExpectedTimeSpeechWithSuffix = $"a la {specificDate.ToString(spanishSpainPattern)}",
                },

                new DateTimeTestData(spanishSpainCulture)
                {
                    InputDateTime = specificDatePluralHour,
                    ExpectedDateSpeech = "viernes 04 de abril", ExpectedDateSpeechWithSuffix = "el viernes 04 de abril",
                    ExpectedTimeSpeech = specificDatePluralHour.ToString(spanishSpainPattern), ExpectedTimeSpeechWithSuffix = $"a las {specificDatePluralHour.ToString(spanishSpainPattern)}",
                },

                // Spanish from Mexico (uses AM PM)
                new DateTimeTestData(spanishMexicoCulture)
                {
                    InputDateTime = specificDate,
                    ExpectedDateSpeech = "viernes 04 de abril", ExpectedDateSpeechWithSuffix = "el viernes 04 de abril",
                    ExpectedTimeSpeech = specificDate.ToString(spanishMexicoPattern), ExpectedTimeSpeechWithSuffix = $"a la {specificDate.ToString(spanishMexicoPattern)}",
                },

                new DateTimeTestData(spanishMexicoCulture)
                {
                    InputDateTime = specificDatePluralHour,
                    ExpectedDateSpeech = "viernes 04 de abril", ExpectedDateSpeechWithSuffix = "el viernes 04 de abril",
                    ExpectedTimeSpeech = specificDatePluralHour.ToString(spanishMexicoPattern), ExpectedTimeSpeechWithSuffix = $"a las {specificDatePluralHour.ToString(spanishMexicoPattern)}",
                },
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
            var cultures = new[] { "en-US", "es-ES", "es-MX", "de-DE", "it", "zh", "fr" };
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