using System;
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
        public void DateTimeToSpeech()
        {
            var date = DateTime.Today;
            Assert.AreEqual("today", date.ToSpeechDateString());

            date = date.AddDays(1);
            Assert.AreEqual("tomorrow", date.ToSpeechDateString());
        }

        [TestMethod]
        public void ToSpeechDateString()
        {
            var cultures = new[] {"en-US", "es-ES", "es-MX", "de-DE"};
            foreach (var culture in cultures)
            {
                CultureInfo.CurrentUICulture = new CultureInfo(culture);
                Assert.AreEqual(CommonStrings.Today, DateTime.UtcNow.ToSpeechDateString());
                Assert.AreEqual(CommonStrings.Tomorrow, DateTime.UtcNow.AddDays(1).ToSpeechDateString());
                Assert.AreEqual(DateTime.UtcNow.AddDays(3).ToString(CommonStrings.SpokenDateFormat), DateTime.UtcNow.AddDays(3).ToSpeechDateString());
                if (string.IsNullOrEmpty(CommonStrings.SpokenDatePrefix))
                {
                    Assert.AreEqual($"{DateTime.UtcNow.AddDays(3).ToString(CommonStrings.SpokenDateFormat)}", DateTime.UtcNow.AddDays(3).ToSpeechDateString(true));
                }
                else
                {
                    Assert.AreEqual($"{CommonStrings.SpokenDatePrefix} {DateTime.UtcNow.AddDays(3).ToString(CommonStrings.SpokenDateFormat)}", DateTime.UtcNow.AddDays(3).ToSpeechDateString(true));
                }
            }
        }
    }
}