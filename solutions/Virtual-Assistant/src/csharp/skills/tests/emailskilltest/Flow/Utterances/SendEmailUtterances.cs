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
            this.Add(SendEmailToRecipient, CreateIntent(
                SendEmailToRecipient,
                intent: Email.Intent.SendEmail,
                contactName: new string[] { ContextStrings.TestRecipient }));
            this.Add(SendEmailToRecipientWithSubject, CreateIntent(
                SendEmailToRecipientWithSubject,
                intent: Email.Intent.SendEmail,
                contactName: new string[] { ContextStrings.TestRecipient },
                subject: new string[] { ContextStrings.TestSubjcet }));
            this.Add(SendEmailToRecipientWithSubjectAndContext, CreateIntent(
                SendEmailToRecipientWithSubjectAndContext,
                intent: Email.Intent.SendEmail,
                contactName: new string[] { ContextStrings.TestRecipient },
                subject: new string[] { ContextStrings.TestSubjcet },
                message: new string[] { ContextStrings.TestContent }));
            this.Add(SendEmailToMultiRecipient, CreateIntent(
                SendEmailToRecipientWithSubjectAndContext,
                intent: Email.Intent.SendEmail,
                contactName: new string[] { ContextStrings.TestRecipient, ContextStrings.TestRecipientWithDup }));
        }

        public static string SendEmails { get; } = "Send Emails";

        public static string SendEmailToEmailAdress { get; } = "Send email to " + ContextStrings.TestEmailAdress;

        public static string SendEmailToNobody { get; } = "Send email to " + ContextStrings.Nobody;

        public static string SendEmailToRecipient { get; } = "Send email to " + ContextStrings.TestRecipient;

        public static string SendEmailToRecipientWithSubject { get; } = "Send email to " + ContextStrings.TestRecipient + " title is " + ContextStrings.TestSubjcet;

        public static string SendEmailToRecipientWithSubjectAndContext { get; } = "Send email to " + ContextStrings.TestRecipient + " title is " + ContextStrings.TestSubjcet + " saying that " + ContextStrings.TestContent;

        public static string SendEmailToMultiRecipient { get; } = "Send email to " + ContextStrings.TestRecipient + " and " + ContextStrings.TestRecipientWithDup;
    }
}
