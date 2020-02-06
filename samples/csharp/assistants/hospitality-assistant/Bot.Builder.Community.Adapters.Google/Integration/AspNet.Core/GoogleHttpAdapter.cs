using Microsoft.Extensions.Logging;

namespace Bot.Builder.Community.Adapters.Google.Integration.AspNet.Core
{
    public class GoogleHttpAdapter : GoogleAdapter, IGoogleHttpAdapter
    {
        public GoogleHttpAdapter(ILogger logger = null)
            : base(new GoogleAdapterOptions(), logger)
        {
        }

        public GoogleHttpAdapter(GoogleAdapterOptions options = null, ILogger logger = null)
            : base(options, logger)
        {
        }
    }
}
