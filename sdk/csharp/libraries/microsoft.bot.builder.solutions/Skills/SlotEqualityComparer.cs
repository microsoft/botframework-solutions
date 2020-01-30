// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Microsoft.Bot.Builder.Solutions.Skills.Models.Manifest;

namespace Microsoft.Bot.Builder.Solutions.Skills
{
    [Obsolete("This type is being deprecated. To continue using Skill capability please refer to https://aka.ms/botframework-solutions/releases/0_8", false)]
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
