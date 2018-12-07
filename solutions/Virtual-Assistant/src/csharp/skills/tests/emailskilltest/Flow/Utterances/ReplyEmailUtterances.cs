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
        }

        public static string ReplyEmails { get; } = "Reply an Email";

        public static string ReplyEmailsWithContent { get; } = "Reply an Email saying " + ContextStrings.TestContent;
    }
}
