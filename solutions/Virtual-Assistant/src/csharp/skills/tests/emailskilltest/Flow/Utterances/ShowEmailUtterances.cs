using Luis;

namespace EmailSkillTest.Flow.Utterances
{
    public class ShowEmailUtterances : BaseTestUtterances
    {
        public static string ShowEmails { get; } = "Show Emails";

        public ShowEmailUtterances()
        {
            this.Add(ShowEmails, CreateIntent(ShowEmails, intent: Email.Intent.CheckMessages));
        }
    }
}
