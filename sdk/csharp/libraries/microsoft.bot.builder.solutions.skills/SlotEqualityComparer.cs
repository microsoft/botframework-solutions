using System.Collections.Generic;
using Microsoft.Bot.Builder.Skills.Models.Manifest;

namespace Microsoft.Bot.Builder.Skills
{
    public class SlotEqualityComparer : IEqualityComparer<Slot>
    {
        public bool Equals(Slot x, Slot y)
        {
            return x.Name.Equals(y.Name);
        }

        public int GetHashCode(Slot obj)
        {
            return obj.Name.GetHashCode();
        }
    }
}
