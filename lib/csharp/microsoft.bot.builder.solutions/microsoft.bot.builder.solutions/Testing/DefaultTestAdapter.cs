using Microsoft.Bot.Builder.Adapters;
using Microsoft.Bot.Builder.Solutions.Middleware;

namespace Microsoft.Bot.Builder.Solutions.Testing
{
    public class DefaultTestAdapter : TestAdapter
    {
        public DefaultTestAdapter()
            : base(sendTraceActivity: false)
        {
            Use(new EventDebuggerMiddleware());
        }
    }
}