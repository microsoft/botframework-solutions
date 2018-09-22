// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using EmailSkill.Models;

namespace EmailSkill
{
    using System;

    /// <summary>
    /// Class represents bot configuration.
    /// </summary>
    [Serializable]
    public class BotConfig
    {
        /// <summary>
        /// Gets or sets language Models for Dispatcher.
        /// </summary>
        /// <value>
        /// Language Models for Dispatcher.
        /// </value>
        public LanguageModel[] LanguageModels { get; set; }
    }
}
