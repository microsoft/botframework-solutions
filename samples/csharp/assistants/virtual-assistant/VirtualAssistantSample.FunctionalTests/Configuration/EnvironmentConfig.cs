// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Bot.Builder.FunctionalTests.Configuration
{
    internal static class EnvironmentConfig
    {
        public static string TestAppId()
        {
            var testAppId = Environment.GetEnvironmentVariable("TESTAPPID");
            if (string.IsNullOrWhiteSpace(testAppId))
            {
                Assert.Inconclusive("Environment variable 'TestAppId' not found.");
            }

            return testAppId;
        }

        public static string TestAppPassword()
        {
            var testPassword = Environment.GetEnvironmentVariable("TESTPASSWORD");

            if (string.IsNullOrWhiteSpace(testPassword))
            {
                Assert.Inconclusive("Environment variable 'TestPassword' not found.");
            }

            return testPassword;
        }
    }
}
