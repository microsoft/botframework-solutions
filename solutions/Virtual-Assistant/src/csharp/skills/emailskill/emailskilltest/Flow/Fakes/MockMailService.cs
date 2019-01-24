using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EmailSkill.ServiceClients;
using EmailSkillTest.Flow.Strings;
using Microsoft.Bot.Solutions.Data;
using Microsoft.Graph;

namespace EmailSkillTest.Flow.Fakes
{
    public class MockMailService : IMailService
    {
        public MockMailService()
        {
            this.MyMessages = FakeMyMessages();
            this.RepliedMessages = FakeMessages();
        }

        public List<Message> MyMessages { get; set; }

        public List<Message> RepliedMessages { get; set; }

        public Task<List<Message>> ReplyToMessageAsync(string id, string content)
        {
            return Task.FromResult(this.RepliedMessages);
        }

        public Task<List<Message>> GetMyMessagesAsync(DateTime startDateTime, DateTime endDateTime, bool isRead, bool isImportant, bool directlyToMe, string mailAddress)
        {
            var messages = new List<Message>();
            foreach (var message in this.MyMessages)
            {
                if (mailAddress != null)
                {
                    if (message.Sender.EmailAddress.Address.Equals(mailAddress))
                    {
                        messages.Add(message);
                    }
                }
                else
                {
                    messages.Add(message);
                }
            }

            return Task.FromResult(messages);
        }

        public async Task ForwardMessageAsync(string id, string content, List<Recipient> recipients)
        {
            await Task.CompletedTask;
        }

        public async Task SendMessageAsync(string content, string subject, List<Recipient> recipients)
        {
            await Task.CompletedTask;
        }

        public async Task DeleteMessageAsync(string id)
        {
            await Task.CompletedTask;
        }

        public List<Message> FakeMyMessages(int number = 5)
        {
            List<Message> messages = new List<Message>();
            for (int i = 0; i < number; i++)
            {
                var message = FakeMessage(
                    subject: ContextStrings.TestSubject + i,
                    bodyPreview: ContextStrings.TestBody + i,
                    content: ContextStrings.TestBody + i,
                    webLink: ContextStrings.WebLink + i,
                    senderName: ContextStrings.TestSender + i,
                    senderAddress: i + ContextStrings.TestSenderAddress,
                    recipientName: ContextStrings.TestRecipient,
                    recipientAddress: ContextStrings.TestEmailAdress);

                messages.Add(message);
            }

            return messages;
        }

        public List<Message> FakeMessages()
        {
            List<Message> messages = new List<Message>();
            return messages;
        }

        public Message FakeMessage(
            string subject = ContextStrings.TestSubject,
            string bodyPreview = ContextStrings.TestBody,
            string content = ContextStrings.TestBody,
            string webLink = ContextStrings.WebLink,
            string senderName = ContextStrings.TestSender,
            string senderAddress = ContextStrings.TestSenderAddress,
            string recipientName = ContextStrings.TestRecipient,
            string recipientAddress = ContextStrings.TestEmailAdress)
        {
            var message = new Message()
            {
                Subject = subject,
                BodyPreview = bodyPreview,
                Body = new ItemBody()
                {
                    Content = content,
                    ContentType = BodyType.Text,
                },
                ReceivedDateTime = DateTime.UtcNow.AddHours(-1),
                WebLink = webLink,
                Sender = new Recipient()
                {
                    EmailAddress = new EmailAddress()
                    {
                        Name = senderName,
                        Address = senderAddress
                    },
                },
            };

            var recipients = new List<Recipient>();
            var recipient = new Recipient()
            {
                EmailAddress = new EmailAddress(),
            };
            recipient.EmailAddress.Address = recipientAddress;
            recipient.EmailAddress.Name = recipientName;
            recipients.Add(recipient);
            message.ToRecipients = recipients;

            return message;
        }
    }
}