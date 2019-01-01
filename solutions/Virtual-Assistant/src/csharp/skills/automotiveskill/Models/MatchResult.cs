// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using AutomotiveSkill.Common;

namespace AutomotiveSkill.Models
{
    public class MatchResult
    {
        public MatchableBagOfTokens Element { get; set; }

        public double Score { get; set; } = 0.0;
    }
}