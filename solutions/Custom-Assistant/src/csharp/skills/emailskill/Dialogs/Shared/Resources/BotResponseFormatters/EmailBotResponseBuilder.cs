// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace EmailSkill
{
    using Microsoft.Bot.Solutions.Dialogs;
    using Microsoft.Bot.Solutions.Dialogs.BotResponseFormatters;

    /// <summary>
    /// Email bot response builder.
    /// </summary>
    public class EmailBotResponseBuilder : BotResponseBuilder
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EmailBotResponseBuilder"/> class.
        /// </summary>
        public EmailBotResponseBuilder()
            : base()
        {
            this.AddFormatter(new TextBotResponseFormatter());
        }
    }
}
