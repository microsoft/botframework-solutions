using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Bot.Solutions.Authentication
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
    }
}
