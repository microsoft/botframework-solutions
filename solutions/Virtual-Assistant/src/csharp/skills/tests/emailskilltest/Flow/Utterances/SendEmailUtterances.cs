using EmailSkillTest.Flow.Strings;
using Luis;

namespace EmailSkillTest.Flow.Utterances
{
    public class SendEmailUtterances : BaseTestUtterances
    {
        public SendEmailUtterances()
        {
            this.Add(SendEmails, CreateIntent(SendEmails, intent: Email.Intent.SendEmail));
            this.Add(SendEmailToEmailAdress, CreateIntent(
                SendEmailToEmailAdress,
                intent: Email.Intent.SendEmail,
                emailAdress: new string[] { ContextStrings.TestEmailAdress }));
            this.Add(SendEmailToNobody, CreateIntent(
                SendEmailToNobody,
                intent: Email.Intent.SendEmail,
                contactName: new string[] { ContextStrings.Nobody }));
        }

        public static string SendEmails { get; } = "Send Emails";

        public static string SendEmailToEmailAdress { get; } = "Send email to " + ContextStrings.TestEmailAdress;

        public static string SendEmailToNobody { get; } = "Send email to " + ContextStrings.Nobody;
    }
}
