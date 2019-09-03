using System.Collections.Generic;

namespace Microsoft.Bot.Builder.Solutions.Contextual.Models.Strategy
{
    public class FIFOStrategy<T> : IReplacementStrategy<T>
    {
        public void Replace(List<T> items, T newItem)
        {
            items.RemoveAt(0);
            items.Add(newItem);
        }
    }
}
