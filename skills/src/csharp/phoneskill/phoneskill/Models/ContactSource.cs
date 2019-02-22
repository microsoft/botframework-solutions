namespace PhoneSkill.Models
{
    /// <summary>
    /// Where the skill gets the user's contacts from.
    /// </summary>
    public enum ContactSource
    {
        /// <summary>
        /// Microsoft Graph API.
        /// </summary>
        Microsoft,

        /// <summary>
        /// Google People API.
        /// </summary>
        Google,
    }
}
