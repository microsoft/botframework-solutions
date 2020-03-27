// Copyright(c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace DirectLine.Web.Configuration
{
    public class DirectLineWebConfiguration
    {
        /// <summary>
        /// Gets or sets the name of the bot as it will be rendered on the web chat UI.
        /// </summary>
        public string BotName { get; set; }

        /// <summary>
        /// Gets or sets the Direct Line secret that is used to acquire a Direct Line token
        /// used for authorization against the Direct Line endpoint.
        /// </summary>
        /// <remarks>
        /// A Direct Line secret can be acquired from your bot registration's Direct Line channel
        /// configuration. Refer to the following article for additional details:
        /// https://blog.botframework.com/2018/09/01/using-webchat-with-azure-bot-services-authentication/
        /// 
        /// If you intend to use the Direct Line Speech channel, you do not need to include this setting.
        /// </remarks>
        public string DirectLineSecret { get; set; }

        /// <summary>
        /// Gets or sets a flag indicating whether Enhanced Authentication should be enabled when
        /// using the Direct Line channel to communicate with your bot.
        /// </summary>
        /// <remarks>
        /// To utilize Enhanced Authentication, you must first enable it in your bot registration's
        /// Direct Line channel. Refer to the following article for additional details:
        /// https://blog.botframework.com/2018/09/01/using-webchat-with-azure-bot-services-authentication/
        /// </remarks>
        public bool EnableDirectLineEnhancedAuthentication { get; set; }

        /// <summary>
        /// Gets or sets the identifier of the region to which your Speech Service resource is deployed to
        /// within your Azure subscription, e.g. 'westus'.
        /// </summary>
        /// <remarks>
        /// A mapping of Region Names to Region Identifiers can be found at the following location:
        /// https://docs.microsoft.com/en-us/azure/cognitive-services/speech-service/rest-speech-to-text#how-to-get-an-access-token
        ///
        /// SpeechServiceRegionIdentifier, along with SpeechServiceSubscriptionKey, are both required if you
        /// intend to integrate using the Direct Line Speech channel. If you intend to use regular Direct Line,
        /// leave both of these properties as empty or null.
        /// </remarks>
        public string SpeechServiceRegionIdentifier { get; set; }

        /// <summary>
        /// Gets or sets the subscription key associated with your Speech Service resource to be used
        /// for acquiring access tokens.
        /// </summary>
        /// <remarks>
        /// SpeechServiceSubscriptionKey, along with SpeechServiceRegionIdentifier, are both required if you
        /// intend to integrate using the Direct Line Speech channel. If you intend to use regular Direct Line,
        /// leave both of these properties as empty or null.
        /// </remarks>
        public string SpeechServiceSubscriptionKey { get; set; }
    }
}
