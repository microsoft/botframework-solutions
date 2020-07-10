// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace SkillSample.FunctionalTests.Configuration
{
    public class BotTestConfiguration : IBotTestConfiguration
    {
        public BotTestConfiguration(string directLineSecret, string botId)
        {
            if (string.IsNullOrEmpty(botId))
            {
                throw new ArgumentNullException(nameof(botId));
            }

            if (string.IsNullOrEmpty(directLineSecret))
            {
                throw new ArgumentNullException(nameof(directLineSecret));
            }

            BotId = botId;
            DirectLineSecret = directLineSecret;
        }

        public BotTestConfiguration()
        {
        }

        public string BotId { get; private set; }

        public string DirectLineSecret { get; private set; }
    }
}
