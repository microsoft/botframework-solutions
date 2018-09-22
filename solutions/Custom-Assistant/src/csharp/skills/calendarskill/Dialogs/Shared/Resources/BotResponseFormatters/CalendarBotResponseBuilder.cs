// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using Microsoft.Bot.Solutions.Dialogs;
using Microsoft.Bot.Solutions.Dialogs.BotResponseFormatters;

namespace CalendarSkill
{
    /// <summary>
    /// A bot response builder for calendar bot.
    /// </summary>
    public class CalendarBotResponseBuilder : BotResponseBuilder
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CalendarBotResponseBuilder"/> class.
        /// </summary>
        public CalendarBotResponseBuilder()
            : base()
        {
            this.AddFormatter(new TextBotResponseFormatter());
            this.AddFormatter(new PluralizeBotResponseFormatter());
            this.AddFormatter(new BotStringBotResponseFormatter());
        }
    }
}
