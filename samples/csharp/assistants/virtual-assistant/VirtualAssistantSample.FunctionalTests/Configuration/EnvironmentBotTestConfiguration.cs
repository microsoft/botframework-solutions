// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace VirtualAssistantSample.FunctionalTests.Configuration
{
    public class EnvironmentBotTestConfiguration : IBotTestConfiguration
    {
        private const string DirectlineSecretKey = "DIRECTLINE";
        private const string BotIdKey = "BOTID";

        public EnvironmentBotTestConfiguration(string directLineSecretKey, string botIdKey, string userId = null)
        {
            // Load config from environment variables
            DirectLineSecret = Environment.GetEnvironmentVariable(directLineSecretKey);
            if (string.IsNullOrWhiteSpace(DirectLineSecret))
            {
                Assert.Inconclusive($"Environment variable '{directLineSecretKey}' not found.");
            }

            BotId = Environment.GetEnvironmentVariable(botIdKey);
            if (string.IsNullOrWhiteSpace(BotId))
            {
                Assert.Inconclusive($"Environment variable '{botIdKey}' not found.");
            }
        }

        public EnvironmentBotTestConfiguration()
            : this(DirectlineSecretKey, BotIdKey)
        {
        }

        public string BotId { get; private set; }

        public string DirectLineSecret { get; private set; }
    }
}