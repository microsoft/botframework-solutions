using Luis;

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
            else if (userInput == "TestName")
            {
                this.Entities.ContactName = new string[] { "Test test" };
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

            return (Email.Intent.None, 0.0);
        }

        private (Intent intent, double score) SendEmailTestLuisResultMock()
        {
            if (userInput == "Send Email")
            {
                return (Email.Intent.SendEmail, 0.90);
            }
            else if (userInput == "TestName")
            {
                this.Entities.ContactName = new string[] { "Test test" };
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

            return (Email.Intent.None, 0.0);
        }
    }
}