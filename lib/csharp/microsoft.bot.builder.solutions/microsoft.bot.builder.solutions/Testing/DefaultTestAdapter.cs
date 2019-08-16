using Microsoft.Bot.Builder.Adapters;
using Microsoft.Bot.Builder.Solutions.Middleware;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Schema;

namespace Microsoft.Bot.Builder.Solutions.Testing
{
    public class DefaultTestAdapter : TestAdapter
    {
        private const string UserAccountId = "user";
        private const string UserAccountName = "User";
        private const string BotAccountId = "bot";
        private const string BotAccountName = "Bot";
        private const string ConversationId = "conversation";
        private const string ConversationName = "Conversation";
        private const string ServiceUrl = "https://test.com";
        private const string OauthConnection = "Azure Active Directory";

        public DefaultTestAdapter(BotStateSet botStateSet, string connectionName = OauthConnection, string token = OauthConnection)
            : base(sendTraceActivity: false)
        {
            Use(new EventDebuggerMiddleware());

            this.Conversation = new ConversationReference
            {
                ChannelId = Channels.Test,
                ServiceUrl = ServiceUrl,
                User = new ChannelAccount(UserAccountId, UserAccountName),
                Bot = new ChannelAccount(BotAccountId, BotAccountName),
                Conversation = new ConversationAccount(false, ConversationId, ConversationName),
            };

            this.AddUserToken(connectionName, Channels.Test, UserAccountId, token);
        }
    }
}