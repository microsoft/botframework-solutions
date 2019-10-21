using Microsoft.Bot.Builder.Solutions.Contextual.Models.Algorithm;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Microsoft.Bot.Builder.Solutions.Tests.Contextual
{
    [TestClass]
    public class LevenshteinDistanceSimilarityTest
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
        public void TestCalculateSimilarityByChar()
        {
            var source = "frooward an email";
            var target1 = "forward an email";
            var target2 = "send my emails";

            var similarity1 = LevenshteinDistanceSimilarity.CalculateSimilarityByChar(source, target1);
            var similarity2 = LevenshteinDistanceSimilarity.CalculateSimilarityByChar(source, target2);

            Assert.IsTrue(similarity1 > similarity2);
        }

        [TestMethod]
        public void TestCalculateSimilarityByWord()
        {
            var source = "create an email";
            var target1 = "send an email";
            var target2 = "show my emails";

            var similarity1 = LevenshteinDistanceSimilarity.CalculateSimilarityByWord(source, target1);
            var similarity2 = LevenshteinDistanceSimilarity.CalculateSimilarityByWord(source, target2);

            Assert.IsTrue(similarity1 > similarity2);
        }
    }
}
