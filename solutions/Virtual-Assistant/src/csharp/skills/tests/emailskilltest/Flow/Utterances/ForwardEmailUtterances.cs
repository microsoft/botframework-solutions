using Luis;

namespace EmailSkillTest.Flow.Utterances
{
    public class ForwardEmailUtterances : BaseTestUtterances
    {
        public ForwardEmailUtterances()
        {
            this.Add(ForwardEmails, CreateIntent(ForwardEmails, intent: Email.Intent.Forward));
        }

        public static string ForwardEmails { get; } = "Forward Email";
    }
}
