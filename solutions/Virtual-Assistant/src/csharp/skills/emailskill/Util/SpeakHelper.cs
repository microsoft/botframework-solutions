using System.Collections.Generic;
using System.Collections.Specialized;
using EmailSkill.Dialogs.Shared.Resources;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Solutions.Extensions;
using Microsoft.Bot.Solutions.Resources;
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
                        speakString += CommonStrings.And + $" {messages[i].Subject} " + CommonStrings.From + $" {messages[i].Sender.EmailAddress.Name}";
                    }
                    else
                    {
                        speakString += $", {messages[i].Subject} " + CommonStrings.From + $" {messages[i].Sender}";
                    }
                }
            }

            return speakString;
        }

        public static string ToSpeechEmailDetailString(Message message)
        {
            string speakString = string.Empty;

            if (message != null)
            {
                speakString = $"{message.Subject}" + CommonStrings.From + $"{message.Sender.EmailAddress.Name}";
            }

            return speakString;
        }

        public static string ToSpeechEmailSendDetailString(string subjects, string toRecipients, string content)
        {
            string speakString = string.Empty;

            speakString = subjects + CommonStrings.To + toRecipients + CommonStrings.Content + content;

            return speakString;
        }

        public static string ToSpeechRecipientsString(IEnumerable<Recipient> recipients)
        {
            string speakString = string.Empty;

            foreach (var recipient in recipients)
            {
                speakString += string.Join(" ", recipient.EmailAddress.Name);
            }

            return speakString;
        }

        private static string ToSpeechEmailSummaryString(WaterfallStepContext sc, Message message)
        {
            string speakString = string.Empty;

            if (message != null)
            {
                if (message.Subject != null && message.Sender != null)
                {
                    speakString = $"{message.Subject} " + CommonStrings.From + $" {message.Sender.EmailAddress.Name}";
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
