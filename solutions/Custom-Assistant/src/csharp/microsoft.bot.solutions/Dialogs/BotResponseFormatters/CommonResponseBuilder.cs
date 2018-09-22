// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Bot.Solutions.Dialogs.BotResponseFormatters
{
    public class CommonResponseBuilder : BotResponseBuilder
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CommonResponseBuilder"/> class.
        /// </summary>
        public CommonResponseBuilder()
            : base()
        {
            this.AddFormatter(new TextBotResponseFormatter());
        }
    }
}
