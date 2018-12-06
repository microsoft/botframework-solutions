using Luis;

namespace EmailSkillTest.Flow.Utterances
{
    public class ReplyEmailUtterances : BaseTestUtterances
    {
        public ReplyEmailUtterances()
        {
            this.Add(ReplyEmails, CreateIntent(ReplyEmails, intent: Email.Intent.Reply));
        }

        public static string ReplyEmails { get; } = "Reply an Email";
    }
}
