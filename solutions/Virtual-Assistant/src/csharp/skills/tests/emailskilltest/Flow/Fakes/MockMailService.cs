using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EmailSkill;
using Microsoft.Graph;

namespace EmailSkillTest.Flow.Fakes
{
    public class MockMailService : IMailService
    {
        public MockMailService()
        {
            this.MyMessages = FakeMyMessages();
            this.RepliedMessages = FakeRepliedMessages();
        }

        public List<Message> MyMessages { get; set; }

        public List<Message> RepliedMessages { get; set; }

        public async Task<List<Message>> ReplyToMessageAsync(string id, string content)
        {
            return this.RepliedMessages;
        }

        public async Task<List<Message>> GetMyMessagesAsync(DateTime startDateTime, DateTime endDateTime, bool isRead, bool isImportant, bool directlyToMe, string mailAddress, int skip)
        {
            return this.MyMessages;
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

        private List<Message> FakeMyMessages()
        {
            List<Message> messages = new List<Message>();
            for (int i = 0; i < 5; i++)
            {
                var message = new Message()
                {
                    Subject = "TestSubject" + i,
                    BodyPreview = "TestBodyPreview" + i,
                    Body = new ItemBody()
                    {
                        Content = "TestBody" + i,
                        ContentType = BodyType.Text,
                    },
                    ReceivedDateTime = DateTime.UtcNow.AddHours(-1),
                    WebLink = "http://www.test.com",
                    Sender = new Recipient()
                    {
                        EmailAddress = new EmailAddress()
                        {
                            Name = "TestSender" + i,
                        },
                    },
                };

                var recipients = new List<Recipient>();
                var recipient = new Recipient()
                {
                    EmailAddress = new EmailAddress(),
                };
                recipient.EmailAddress.Address = i + "test@test.com";
                recipient.EmailAddress.Name = "Test Test";
                recipients.Add(recipient);
                message.ToRecipients = recipients;

                messages.Add(message);
            }

            return messages;
        }

        private List<Message> FakeRepliedMessages()
        {
            List<Message> messages = new List<Message>();
            return messages;
        }
    }
}
