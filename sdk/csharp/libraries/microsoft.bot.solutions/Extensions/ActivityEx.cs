// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Linq;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Schema;

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
                case Channels.DirectlineSpeech:
                case Channels.Test:
                    {
                        if (activity.Type == ActivityTypes.ConversationUpdate)
                        {
                            // When bot is added to the conversation (triggers start only once per conversation)
                            if (activity.MembersAdded != null && activity.MembersAdded.Any(m => m.Id == activity.Recipient.Id))
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
    }
}