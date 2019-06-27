using Luis;

namespace EmailSkillTest.Flow.Utterances
{
    public class DeleteEmailUtterances : BaseTestUtterances
    {
        public DeleteEmailUtterances()
        {
            this.Add(DeleteEmails, CreateIntent(DeleteEmails, intent: emailLuis.Intent.Delete));
            this.Add(DeleteEmailsWithSelection, CreateIntent(
               DeleteEmailsWithSelection,
               intent: emailLuis.Intent.Delete,
               ordinal: new double[] { 2 }));
            this.Add(DeleteCurrentEmail, CreateIntent(DeleteCurrentEmail, intent: emailLuis.Intent.Delete));
        }

        public static string DeleteEmails { get; } = "Delete an Email";

        public static string DeleteEmailsWithSelection { get; } = "Delete the second Email";

        public static string DeleteCurrentEmail { get; } = "Delete the current Email";
    }
}
