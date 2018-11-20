using Microsoft.Bot.Connector;
using Microsoft.Bot.Schema;
using System;
using System.Linq;

namespace $safeprojectname$.Extensions
{
    public static class ActivityExtensions
    {
        public static bool IsStartActivity(this Activity activity)
        {
            switch (activity.ChannelId)
            {
                case Channels.Cortana:
                    {
                        var intent = activity.Entities?.FirstOrDefault(entity => entity.Type.Equals("Intent", StringComparison.Ordinal));
                        var intentName = string.Empty;
                        if (intent != null)
                        {
                            intentName = intent.Properties["name"]?.ToString();
                        }

                        return (intent != null
                                && !string.IsNullOrEmpty(intentName)
                                && intentName == "Microsoft.Launch");
                    }

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
    }
}
