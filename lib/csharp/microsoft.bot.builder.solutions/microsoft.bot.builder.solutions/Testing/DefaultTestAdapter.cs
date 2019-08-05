using Microsoft.Bot.Builder.Adapters;
using Microsoft.Bot.Builder.Solutions.Middleware;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Schema;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Bot.Builder.Solutions.Testing
{
    public class DefaultTestAdapter : TestAdapter
    {
        private const string userAccountId = "user";
        private const string userAccountName = "User";
        private const string botAccountId = "bot";
        private const string botAccountName = "Bot";
        private const string conversationId = "conversation";
        private const string conversationName = "Conversation";
        private const string serviceUrl = "https://test.com";
        private const string oauthConnection = "Azure Active Directory";

        public DefaultTestAdapter(BotStateSet botStateSet, string connectionName = oauthConnection, string token = oauthConnection)
            : base(sendTraceActivity: false)
        {
            Use(new EventDebuggerMiddleware());
            Use(new AutoSaveStateMiddleware(botStateSet));

            this.Conversation = new ConversationReference
            {
                ChannelId = Channels.Test,
                ServiceUrl = serviceUrl,
                User = new ChannelAccount(userAccountId, userAccountName),
                Bot = new ChannelAccount(botAccountId, botAccountName),
                Conversation = new ConversationAccount(false, conversationId, conversationName),
            };

            this.AddUserToken(connectionName, Channels.Test, userAccountId, token);
        }
    }
}