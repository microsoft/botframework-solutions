using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Bot.Solutions.Extensions;
using Microsoft.Bot.Solutions.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace Microsoft.Bot.Solutions.Tests.Extensions
{
    [TestClass]
    [TestCategory("UnitTests")]
    [ExcludeFromCodeCoverageAttribute]
    public class LuisRecognizeExTest
    {
        private const string _sentiment = "sentiment";

        [TestMethod]
        public void GetSentimentInfoWithSentimentEnabled()
        {
            var skillLuis = new SkillLuis();
            skillLuis.Properties = new Dictionary<string, object>();
            skillLuis.Properties.Add(new KeyValuePair<string, object>(_sentiment, "{\"label\": \"positive\", \"score\": 0.91}"));

            (var type, var score) = skillLuis.GetSentimentInfo(li => li.Properties);
            Assert.AreEqual(SentimentType.Positive, type);
            Assert.AreEqual(0.91, score);
        }

        [TestMethod]
        public void GetSentimentInfoWithSentimentNotEnabled()
        {
            var skillLuis = new SkillLuis();

            (var type, var score) = skillLuis.GetSentimentInfo(li => li.Properties);
            Assert.AreEqual(SentimentType.None, type);
            Assert.AreEqual(0.0, score);
        }

        private class SkillLuis
        {
            [JsonExtensionData(ReadData = true, WriteData = true)]
            public IDictionary<string, object> Properties { get; set; }
        }
    }
}
