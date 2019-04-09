using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Adapters;

namespace VirtualAssistantTemplate.Tests.Adapters
{
    public class DefaultTestAdapter : TestAdapter
    {
        public DefaultTestAdapter(BotStateSet botStateSet)
        {
            Use(new AutoSaveStateMiddleware(botStateSet));
        }
    }
}
