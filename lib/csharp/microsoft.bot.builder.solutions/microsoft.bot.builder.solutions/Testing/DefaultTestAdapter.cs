using Microsoft.Bot.Builder.Adapters;

namespace Microsoft.Bot.Builder.Solutions.Testing
{
    public class DefaultTestAdapter : TestAdapter
    {
        public DefaultTestAdapter(BotStateSet botStateSet)
        {
            Use(new AutoSaveStateMiddleware(botStateSet));
        }
    }
}
