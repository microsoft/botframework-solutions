// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

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
    public class ListExTests
    {
        [TestMethod]
        public void Defaults()
        {
            CultureInfo.CurrentUICulture = new CultureInfo("en-us");

            // Default is ToString and final separator is "and"
            var testList = new List<string> { "One", "Two", "Three" };
            Assert.AreEqual("One, Two and Three", testList.ToSpeechString(CommonStrings.And));
        }

        [TestMethod]
        public void ToSpeechString()
        {
            CultureInfo.CurrentUICulture = new CultureInfo("en-us");

            var testList = new List<SomeComplexType>();

            Assert.AreEqual(string.Empty, testList.ToSpeechString(CommonStrings.Or, li => li.Number));

            testList.Add(new SomeComplexType { Number = "One", SomeOtherProperty = "Don't care" });
            Assert.AreEqual("One", testList.ToSpeechString(CommonStrings.Or, li => li.Number));

            testList.Add(new SomeComplexType { Number = "Two", SomeOtherProperty = "Don't care" });
            Assert.AreEqual("One or Two", testList.ToSpeechString(CommonStrings.Or, li => li.Number));

            testList.Add(new SomeComplexType { Number = "Three", SomeOtherProperty = "Don't care" });
            Assert.AreEqual("One, Two or Three", testList.ToSpeechString(CommonStrings.Or, li => li.Number));

            testList.Add(new SomeComplexType { Number = "Four", SomeOtherProperty = "Don't care" });
            Assert.AreEqual("One, Two, Three or Four", testList.ToSpeechString(CommonStrings.Or, li => li.Number));
        }

        private class SomeComplexType
        {
            public string Number { get; set; }

            public object SomeOtherProperty { get; set; }
        }
    }
}