using Luis;
using Microsoft.Bot.Builder.AI.Luis;

namespace EmailSkillTest.Flow.Fakes
{
    public class MockEmailIntent : Email
    {
        private string userInput;
        private Intent intent;
        private double score;

        public MockEmailIntent(string userInput)
        {
            this.Entities = new Email._Entities();
            this.userInput = userInput;

            this.intent = Email.Intent.None;
            this.score = 0;

            (intent, score) = ShowEmailTestLuisResultMock();

            if (intent == Email.Intent.None)
            {
                (intent, score) = SendEmailTestLuisResultMock();
            }

            if (intent == Email.Intent.None)
            {
                (intent, score) = ReplyEmailTestLuisResultMock();
            }

            if (intent == Email.Intent.None)
            {
                (intent, score) = ForwardEmailTestLuisResultMock();
            }
        }

        public override _Entities Entities { get; set; }

        public override (Intent intent, double score) TopIntent()
        {
            return (intent, score);
        }

        private (Intent intent, double score) ForwardEmailTestLuisResultMock()
        {
            if (userInput == "Forward Email")
            {
                return (Email.Intent.Forward, 0.90);
            }
            else if (userInput == "The first one")
            {
                this.Entities.ordinal = new double[] { 1 };
                return (Email.Intent.SelectItem, 0.90);
            }
            else if (userInput == "Test Test")
            {
                this.Entities.ContactName = new string[] { "Test Test" };
                return (Email.Intent.None, 0.90);
            }
            else if (userInput == "TestContent")
            {
                this.Entities.Message = new string[] { "Test message" };
                return (Email.Intent.None, 0.90);
            }

            return (Email.Intent.None, 0.0);
        }

        private (Intent intent, double score) ReplyEmailTestLuisResultMock()
        {
            if (userInput == "Reply an Email")
            {
                return (Email.Intent.Reply, 0.90);
            }
            else if (userInput == "The first email")
            {
                this.Entities.ordinal = new double[] { 1 };
                return (Email.Intent.SelectItem, 0.90);
            }
            else if (userInput == "TestContent")
            {
                this.Entities.Message = new string[] { "Test message" };
                return (Email.Intent.None, 0.90);
            }

            return (Email.Intent.None, 0.0);
        }

        private (Intent intent, double score) ShowEmailTestLuisResultMock()
        {
            if (userInput == "Show Emails")
            {
                return (Email.Intent.SearchMessages, 0.90);
            }
            else if (userInput == "The first one")
            {
                this.Entities.ordinal = new double[] { 1 };
                return (Email.Intent.SelectItem, 0.90);
            }
            else if (userInput == "1")
            {
                this.Entities.number = new double[] { 1 };
                return (Email.Intent.SelectItem, 0.90);
            }

            return (Email.Intent.None, 0.0);
        }

        private (Intent intent, double score) SendEmailTestLuisResultMock()
        {
            if (userInput == "Send Email")
            {
                return (Email.Intent.SendEmail, 0.90);
            }
            else if (userInput == "Test Test")
            {
                this.Entities.ContactName = new string[] { "Test Test" };
                return (Email.Intent.None, 0.90);
            }
            else if (userInput == "TestDup Test")
            {
                this.Entities.ContactName = new string[] { "TestDup Test" };
                return (Email.Intent.None, 0.90);
            }
            else if (userInput == "TestSubjcet")
            {
                this.Entities.EmailSubject = new string[] { "Test" };
                return (Email.Intent.None, 0.90);
            }
            else if (userInput == "TestContent")
            {
                this.Entities.Message = new string[] { "Test message" };
                return (Email.Intent.None, 0.90);
            }
            else if (userInput == "The first one")
            {
                this.Entities.ordinal = new double[] { 1 };
                return (Email.Intent.SelectItem, 0.90);
            }
            else if (userInput == "1")
            {
                this.Entities.number = new double[] { 1 };
                return (Email.Intent.SelectItem, 0.90);
            }
            else if (userInput == "Send email to test@test.com")
            {
                this.Entities.EmailAddress = new string[] { "test@test.com" };

                this.Text = "Send email to test@test.com";
                this.Entities._instance = new _Entities._Instance();
                InstanceData testEmail = new InstanceData()
                {
                    StartIndex = 14,
                    EndIndex = 27
                };
                this.Entities._instance.EmailAddress = new InstanceData[1] { testEmail };

                return (Email.Intent.SendEmail, 0.90);
            }
            else if (userInput == "Send email to Nobody")
            {
                this.Entities.ContactName = new string[] { "Nobody" };
                return (Email.Intent.SendEmail, 0.90);
            }
            else if (userInput == "test@test.com")
            {
                this.Entities.EmailAddress = new string[] { "test@test.com" };

                this.Text = "test@test.com";
                this.Entities._instance = new _Entities._Instance();
                InstanceData testEmail = new InstanceData()
                {
                    StartIndex = 0,
                    EndIndex = 13
                };
                this.Entities._instance.EmailAddress = new InstanceData[1] { testEmail };

                return (Email.Intent.SelectItem, 0.90);
            }

            return (Email.Intent.None, 0.0);
        }
    }
}