using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Bot.Builder.Solutions.Contextual.Services.Sentiment
{
    public class RequestStatistics
    {
        /// <summary>
        /// Number of documents submitted in the request.
        /// </summary>
        public int DocumentsCount { get; set; }

        /// <summary>
        /// Number of valid documents. This excludes empty, over-size limit or non-supported languages documents.
        /// </summary>
        public int ValidDocumentsCount { get; set; }

        /// <summary>
        /// Number of invalid documents. This includes empty, over-size limit or non-supported languages documents.
        /// </summary>
        public int ErroneousDocumentsCount { get; set; }

        /// <summary>
        /// Number of transactions for the request.
        /// </summary>
        public long TransactionsCount { get; set; }
    }
}
