using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Bot.Builder.Solutions.Contextual.Services.Sentiment
{
    public class TextAnalyticsBatchInput
    {
        public IList<TextAnalyticsInput> Documents { get; set; }
    }
}
