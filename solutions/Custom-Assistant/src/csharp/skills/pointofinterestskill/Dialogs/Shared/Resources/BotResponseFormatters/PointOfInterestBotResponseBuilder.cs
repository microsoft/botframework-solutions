// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using Microsoft.Bot.Solutions.Dialogs;
using Microsoft.Bot.Solutions.Dialogs.BotResponseFormatters;

namespace PointOfInterestSkill
{
    /// <summary>
    /// A bot response builder for calendar bot.
    /// </summary>
    public class PointOfInterestBotResponseBuilder : BotResponseBuilder
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PointOfInterestBotResponseBuilder"/> class.
        /// </summary>
        public PointOfInterestBotResponseBuilder()
            : base()
        {
            AddFormatter(new TextBotResponseFormatter());
            AddFormatter(new PluralizeBotResponseFormatter());
            AddFormatter(new BotStringBotResponseFormatter());
        }
    }
}
