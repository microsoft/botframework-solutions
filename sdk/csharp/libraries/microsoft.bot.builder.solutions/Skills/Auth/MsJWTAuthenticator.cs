using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Bot.Builder.Solutions.Skills.Auth
{
    public class MsJWTAuthenticator : Authenticator
    {
        public MsJWTAuthenticator(BotSettingsBase botSettingsBase, IWhitelistAuthenticationProvider whitelistAuthenticationProvider)
            : base(new MsJWTAuthenticationProvider(AsMicrosoftAppId(botSettingsBase)), whitelistAuthenticationProvider)
        {
        }

        private static string AsMicrosoftAppId(BotSettingsBase botSettingsBase)
        {
            var settings = botSettingsBase ?? throw new ArgumentNullException(nameof(botSettingsBase));
            return settings.MicrosoftAppId;
        }
    }
}
