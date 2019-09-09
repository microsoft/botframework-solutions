// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Microsoft.Bot.Builder.Skills.Models.Manifest;

namespace Microsoft.Bot.Builder.Skills
{
    public class SlotEqualityComparer : IEqualityComparer<Slot>
    {
        public bool Equals(Slot x, Slot y)
            => x.Name.Equals(y.Name, StringComparison.InvariantCulture);

        public int GetHashCode(Slot obj)
            => obj.Name.GetHashCode();
    }
}
