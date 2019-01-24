using System;
using System.Collections.Generic;
using EmailSkill.Dialogs.Shared.Resources.Strings;
using EmailSkill.Extensions;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Solutions.Extensions;
using Microsoft.Bot.Solutions.Resources;
using Microsoft.Graph;

namespace EmailSkill.Util
{
    public class SpeakHelper
    {
        public static string ToSpeechEmailListString(List<Message> messages, TimeZoneInfo timeZone, int maxReadSize)
        {
            string speakString = string.Empty;

            if (messages == null || messages.Count == 0)
            {
                return speakString;
            }

            List<string> emailDetails = new List<string>();

            int readSize = Math.Min(messages.Count, maxReadSize);
            if (readSize == 1)
            {
                var emailDetail = ToSpeechEmailDetailOverallString(messages[0], timeZone);
                emailDetails.Add(emailDetail);
            }
            else
            {
                for (int i = 0; i < readSize; i++)
                {
                    var readFormat = string.Empty;

                    if (i == 0)
                    {
                        readFormat = CommonStrings.FirstItem;
                    }
                    else if (i == 1)
                    {
                        readFormat = CommonStrings.SecondItem;
                    }
                    else if (i == 2)
                    {
                        readFormat = CommonStrings.ThirdItem;
                    }

                    var emailDetail = string.Format(readFormat, ToSpeechEmailDetailOverallString(messages[i], timeZone));
                    emailDetails.Add(emailDetail);
                }
            }

            speakString = emailDetails.ToSpeechString(CommonStrings.And);
            return speakString;
        }

        public static string ToSpeechEmailDetailOverallString(Message message, TimeZoneInfo timeZone)
        {
            string speakString = string.Empty;

            if (message != null)
            {
                string time = message.ReceivedDateTime == null
                    ? CommonStrings.NotAvailable
                    : message.ReceivedDateTime.Value.UtcDateTime.ToRelativeString(timeZone);
                string sender = (message.Sender?.EmailAddress?.Name != null) ? message.Sender.EmailAddress.Name : EmailCommonStrings.UnknownSender;
                speakString = string.Format(EmailCommonStrings.FromDetailsFormat, sender, time);
            }

            return speakString;
        }

        public static string ToSpeechEmailDetailString(Message message, TimeZoneInfo timeZone)
        {
            string speakString = string.Empty;

            if (message != null)
            {
                string time = message.ReceivedDateTime == null
                    ? CommonStrings.NotAvailable
                    : message.ReceivedDateTime.Value.UtcDateTime.ToRelativeString(timeZone);
                string subject = message.Subject ?? EmailCommonStrings.EmptySubject;
                string sender = (message.Sender?.EmailAddress?.Name != null) ? message.Sender.EmailAddress.Name : EmailCommonStrings.UnknownSender;
                string content = message.BodyPreview ?? EmailCommonStrings.EmptyContent;
                speakString = string.Format(EmailCommonStrings.FromDetailsFormatAll, sender, time, subject, content);
            }

            return speakString;
        }

        public static string ToSpeechEmailSendDetailString(string detailSubject, string detailToRecipient, string detailContent)
        {
            string speakString = string.Empty;

            string subject = (detailSubject != string.Empty) ? detailSubject : EmailCommonStrings.EmptySubject;
            string toRecipient = (detailToRecipient != string.Empty) ? detailToRecipient : EmailCommonStrings.UnknownRecipient;
            string content = (detailContent != string.Empty) ? detailContent : EmailCommonStrings.EmptyContent;

            speakString = string.Format(EmailCommonStrings.ToDetailsFormat, subject, toRecipient, content);

            return speakString;
        }

        public static string ToSpeechSelectionDetailString(PromptOptions selectOption, int maxSize)
        {
            var result = string.Empty;
            result += selectOption.Prompt.Text + "\r\n";

            List<string> selectionDetails = new List<string>();

            int readSize = Math.Min(selectOption.Choices.Count, maxSize);
            if (readSize == 1)
            {
                selectionDetails.Add(selectOption.Choices[0].Value);
            }
            else
            {
                for (var i = 0; i < readSize; i++)
                {
                    var readFormat = string.Empty;

                    if (i == 0)
                    {
                        readFormat = CommonStrings.FirstItem;
                    }
                    else if (i == 1)
                    {
                        readFormat = CommonStrings.SecondItem;
                    }
                    else if (i == 2)
                    {
                        readFormat = CommonStrings.ThirdItem;
                    }

                    var selectionDetail = string.Format(readFormat, selectOption.Choices[i].Value);
                    selectionDetails.Add(selectionDetail);
                }
            }

            result += selectionDetails.ToSpeechString(CommonStrings.And);
            return result;
        }
    }
}
