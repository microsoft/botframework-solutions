using System.Collections.Generic;
using Microsoft.Bot.Solutions.Extensions;
using Microsoft.Bot.Solutions.Resources;
using Microsoft.Graph;

namespace EmailSkill.Util
{
    public class SpeakHelper
    {
        public static string ToSpeechEmailListString(List<Message> messages)
        {
            string speakString = string.Empty;

            if (messages == null || messages.Count == 0)
            {
                return speakString;
            }

            List<string> emailDetails = new List<string>();
            for (int i = 0; i < messages.Count; i++)
            {
                var emailDetail = ToSpeechEmailDetailString(messages[i]);
                emailDetails.Add(emailDetail);
            }

            speakString = emailDetails.ToSpeechString(CommonStrings.And);
            return speakString;
        }

        public static string ToSpeechEmailDetailString(Message message)
        {
            string speakString = string.Empty;

            if (message != null)
            {
                string subject = (message.Subject != null) ? message.Subject : CommonStrings.EmptySubject;
                string sender = (message.Sender?.EmailAddress?.Name != null) ? message.Sender.EmailAddress.Name : CommonStrings.UnknownSender;

                speakString = string.Format(CommonStrings.FromDetailsFormat, subject, sender);
            }

            return speakString;
        }

        public static string ToSpeechEmailSendDetailString(string detailSubject, string detailToRecipient, string detailContent)
        {
            string speakString = string.Empty;

            string subject = (detailSubject != string.Empty) ? detailSubject : CommonStrings.EmptySubject;
            string toRecipient = (detailToRecipient != string.Empty) ? detailToRecipient : CommonStrings.UnknownRecipient;
            string content = (detailContent != string.Empty) ? detailContent : CommonStrings.EmptyContent;

            speakString = string.Format(CommonStrings.ToDetailsFormat, subject, toRecipient, content);

            return speakString;
        }
    }
}
