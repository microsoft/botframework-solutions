// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Text;

namespace ITSMSkill.Tests.Flow.Utterances
{
    public abstract class BaseTestUtterances<T> : Dictionary<string, T>
    {
        public static double TopIntentScore { get; } = 0.9;

        public abstract T NoneIntent { get; }
    }
}
