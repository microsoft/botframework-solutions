using System;
using System.Collections.Generic;

namespace Microsoft.Bot.Builder.Solutions.Contextual.Models.Strategy
{
    public class RandomStrategy<T> : IReplacementStrategy<T>
    {
        public void Replace(List<T> items, T item)
        {
            var rand = new Random();
            items.RemoveAt(rand.Next(items.Count));
            items.Add(item);
        }
    }
}
