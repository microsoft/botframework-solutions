// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions.Cards;
using Microsoft.Bot.Solutions.Dialogs;

namespace Microsoft.Bot.Solutions.Extensions
{
    /// <summary>
    /// Extension methods for the Activity class.
    /// </summary>
    public static class ActivityEx
    {
        public static bool IsStartActivity(this Activity activity)
        {
            switch (activity.ChannelId)
            {
                case Channels.Skype:
                    {
                        if (activity.Type == ActivityTypes.ContactRelationUpdate && activity.Action == "add")
                        {
                            return true;
                        }

                        return false;
                    }

                case Channels.Directline:
                case Channels.Emulator:
                case Channels.Webchat:
                case Channels.Msteams:
                    {
                        if (activity.Type == ActivityTypes.ConversationUpdate)
                        {
                            // When bot is added to the conversation (triggers start only once per conversation)
                            if (activity.MembersAdded.Any(m => m.Id == activity.Recipient.Id))
                            {
                                return true;
                            }
                        }

                        return false;
                    }

                default:
                    {
                        return false;
                    }
            }
        }

        public static Activity CreateReply(this Activity activity, BotResponse response, BotResponseBuilder responseBuilder = null, StringDictionary tokens = null)
        {
            var reply = activity.CreateReply();
            if (responseBuilder == null)
            {
                responseBuilder = BotResponseBuilder();
            }

            responseBuilder.BuildMessageReply(reply, response, tokens);
            return reply;
        }

        public static Activity CreateAdaptiveCardGroupReply<T>(this Activity activity, BotResponse response, string cardPath, string attachmentLayout, List<T> cardDataAdapters, BotResponseBuilder responseBuilder = null, StringDictionary tokens = null)
            where T : CardDataBase
        {
            var reply = activity.CreateReply();
            if (responseBuilder == null)
            {
                responseBuilder = BotResponseBuilder();
            }

            responseBuilder.BuildAdaptiveCardGroupReply(reply, response, cardPath, attachmentLayout, cardDataAdapters, tokens);
            return reply;
        }

        public static Activity CreateAdaptiveCardReply<T>(this Activity activity, BotResponse response, string cardPath, T cardDataAdapter, BotResponseBuilder responseBuilder = null, StringDictionary tokens = null, Activity replyToUse = null)
            where T : CardDataBase
        {
            var reply = replyToUse ?? activity.CreateReply();
            if (responseBuilder == null)
            {
                responseBuilder = BotResponseBuilder();
            }

            responseBuilder.BuildAdaptiveCardReply(reply, response, cardPath, cardDataAdapter, tokens);
            return reply;
        }

        private static BotResponseBuilder BotResponseBuilder()
        {
            var responseBuilder = new BotResponseBuilder();
            return responseBuilder;
        }
    }
}