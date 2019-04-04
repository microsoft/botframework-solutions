namespace EmailSkill.Models
{
    public class SendEmailStateModel
    {
        public enum ResendEmailState
        {
            /// <summary>
            /// Cancel the recreate
            /// </summary>
            Cancel = 0,

            /// <summary>
            /// Change the Participants and recerate.
            /// </summary>
            Participants = 1,

            /// <summary>
            /// Change the subject and recerate.
            /// </summary>
            Subject = 2,

            /// <summary>
            /// Change the content and recerate.
            /// </summary>
            Content = 3
        }
    }
}
