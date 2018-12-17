// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AutomotiveSkill.Models
{
    public class ScoredMatchableBagOfTokens
    {
        public MatchableBagOfTokens Option { get; set; }

        public double Score { get; set; } = 0.0;
    }
}
