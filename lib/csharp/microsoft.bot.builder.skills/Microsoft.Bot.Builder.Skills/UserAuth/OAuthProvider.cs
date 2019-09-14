namespace Microsoft.Bot.Builder.Solutions.Skills.UserAuth
{
    public enum OAuthProvider
    {
        /// <summary>
        /// Azure Activity Directory authentication provider.
        /// </summary>
        AzureAD,

        /// <summary>
        /// Google authentication provider.
        /// </summary>
        Google,

        /// <summary>
        /// Todoist authentication provider.
        /// </summary>
        Todoist,

        /// <summary>
        /// Generic Oauth 2 provider.
        /// </summary>
        GenericOauth2,
    }
}
