using System.Collections.Generic;
using System.Collections.Specialized;
using EmailSkill.Dialogs.Shared.Resources;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Solutions.Extensions;
using Microsoft.Graph;

namespace EmailSkill.Util
{
    public class SpeakHelper
    {
        public static string ToSpeechEmailListString(WaterfallStepContext sc, List<Message> messages)
        {
            string speakString = string.Empty;

            if (messages != null && messages.Count >= 1)
            {
                speakString = ToSpeechEmailSummaryString(sc, messages[0]);
            }

            for (int i = 1; i < messages.Count; i++)
            {
                if (messages[i].Subject != null && messages[i].Sender != null)
                {
                    if (i == messages.Count - 1)
                    {
                        var andMessage = sc.Context.Activity.CreateReply(EmailSharedResponses.ConnectWords, null, new StringDictionary() { });
                        var fromMessage = sc.Context.Activity.CreateReply(EmailSharedResponses.FromWords, null, new StringDictionary() { });
                        speakString += andMessage.Speak + $" {messages[i].Subject} " + fromMessage.Speak + $" {messages[i].Sender}";
                    }
                    else
                    {
                        var fromMessage = sc.Context.Activity.CreateReply(EmailSharedResponses.FromWords, null, new StringDictionary() { });
                        speakString += $", {messages[i].Subject} " + fromMessage.Speak + $" {messages[i].Sender}";
                    }
                }
            }

            return speakString;
        }

        public static string ToSpeechEmailDetailString(Message message)
        {
            string speakString = string.Empty;

            //if (message != null)
            //{
            //    speakString = $"{message.Subject} from {message.Sender}";
            //}

            return speakString;
        }

        private static string ToSpeechEmailSummaryString(WaterfallStepContext sc, Message message)
        {
            string speakString = string.Empty;

            if (message != null)
            {
                if (message.Subject != null && message.Sender != null)
                {
                    var fromMessage = sc.Context.Activity.CreateReply(EmailSharedResponses.FromWords, null, new StringDictionary() { });

                    speakString = $"{message.Subject} " + fromMessage.Speak + $" {message.Sender}";
                }
                else if (message.Subject != null)
                {
                    speakString = $"{message.Subject} ";
                }
            }

            return speakString;
        }
    }
}
