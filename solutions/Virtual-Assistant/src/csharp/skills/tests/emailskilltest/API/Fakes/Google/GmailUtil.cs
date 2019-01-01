using System.Collections.Generic;
using System.Net.Mail;
using System.Text;
using EmailSkill.ServiceClients.GoogleAPI;
using MimeKit;
using GmailMessage = Google.Apis.Gmail.v1.Data.Message;

namespace EmailSkillTest.API.Fakes.Google
{
    public class GmailUtil
    {
        public static IList<GmailMessage> GetFakeGmailMessageList(int size = 5)
        {
            var messages = new List<GmailMessage>();

            for (int i = 0; i < size; i++)
            {
                var message = GetFakeGmailMessage(to: "test@test.com" + i);
                messages.Add(message);
            }

            return messages;
        }

        public static GmailMessage GetFakeGmailMessage(
            string from = "test@test.com",
            string to = "test@test.com",
            string subject = "test subject",
            string content = "test content")
        {
            var mess = new MailMessage
            {
                Subject = subject,
                From = new MailAddress(from)
            };
            mess.To.Add(new MailAddress(to));

            var adds = AlternateView.CreateAlternateViewFromString(content, new System.Net.Mime.ContentType("text/plain"));
            adds.ContentType.CharSet = Encoding.UTF8.WebName;
            mess.AlternateViews.Add(adds);

            var mime = MimeMessage.CreateFromMailMessage(mess);
            var gmailMessage = new GmailMessage()
            {
                Raw = GMailService.Base64UrlEncode(mime.ToString()),
                ThreadId = "1"
            };

            return gmailMessage;
        }
    }
}