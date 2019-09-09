using System;

namespace Microsoft.Bot.Builder.Solutions.Contextual.Models
{
    public class PreviousInput
    {
        public string Utterance { get; set; }

        public string Intent { get; set; }

        public DateTimeOffset TimeStamp { get; set; }
    }
}
