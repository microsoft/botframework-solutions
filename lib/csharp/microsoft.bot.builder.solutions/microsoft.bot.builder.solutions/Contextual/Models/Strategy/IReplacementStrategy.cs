using System.Collections.Generic;

namespace Microsoft.Bot.Builder.Solutions.Contextual.Models.Strategy
{
    public interface IReplacementStrategy<T>
    {
        void Replace(List<T> items, T newItem);
    }
}
