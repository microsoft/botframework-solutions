// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Bot.Solutions.Dialogs
{
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using Microsoft.Bot.Solutions.Cards;
    using Microsoft.Bot.Solutions.Dialogs.BotResponseFormatters;
    using Microsoft.Bot.Schema;

    public interface IBotResponseBuilder
    {
        void BuildAdaptiveCardReply<T>(Activity reply, BotResponse response, string cardPath, T cardDataAdapter, StringDictionary tokens = null)
            where T : CardDataBase;

        void BuildAdaptiveCardGroupReply<T>(Activity reply, BotResponse response, string cardPath, string attachmentLayout, List<T> cardDataAdapters, StringDictionary tokens = null)
            where T : CardDataBase;

        void BuildYesNoReply(Activity reply, BotResponse response, StringDictionary tokens = null);

        void BuildMessageReply(Activity reply, BotResponse response, StringDictionary tokens = null);

        void AddFormatter(IBotResponseFormatter formatter);
    }
}
