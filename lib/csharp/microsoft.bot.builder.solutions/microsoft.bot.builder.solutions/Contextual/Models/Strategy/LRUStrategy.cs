using System.Collections.Generic;

namespace Microsoft.Bot.Builder.Solutions.Contextual.Models.Strategy
{
    public class LRUStrategy<T> : IReplacementStrategy<T>
    {
        public void Replace(List<T> items, T newItem)
        {
            items.Sort(new LRUQuestionComparer());
            items.RemoveAt(0);
            items.Add(newItem);
        }

        private class LRUQuestionComparer : IComparer<T>
        {
            public int Compare(T x, T y)
            {
                if (typeof(T) == typeof(PreviousTriggerIntent))
                {
                    var itemX = x as PreviousTriggerIntent;
                    var itemY = y as PreviousTriggerIntent;
                    return itemX.TimeStamp.CompareTo(itemY.TimeStamp);
                }
                else
                {
                    return 1;
                }
            }
        }
    }
}
