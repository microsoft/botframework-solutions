using EmailSkillTest.Flow.Strings;
using Luis;

namespace EmailSkillTest.Flow.Utterances
{
    public class ReplyEmailUtterances : BaseTestUtterances
    {
        public ReplyEmailUtterances()
        {
            this.Add(ReplyEmails, CreateIntent(ReplyEmails, intent: Email.Intent.Reply));
            this.Add(ReplyEmailsWithContent, CreateIntent(
                ReplyEmailsWithContent,
                intent: Email.Intent.Reply,
                message: new string[] { ContextStrings.TestContent }));
            this.Add(ReplyEmailsWithSelection, CreateIntent(
               ReplyEmailsWithSelection,
               intent: Email.Intent.Reply,
               ordinal: new double[] { 2 }));
            this.Add(ReplyCurrentEmail, CreateIntent(ReplyCurrentEmail, intent: Email.Intent.Reply));
        }

        public static string ReplyEmails { get; } = "Reply an Email";

        public static string ReplyEmailsWithContent { get; } = "Reply an Email saying " + ContextStrings.TestContent;

        public static string ReplyEmailsWithSelection { get; } = "Reply the second Email";

        public static string ReplyCurrentEmail { get; } = "Reply the current Email";
    }
}
