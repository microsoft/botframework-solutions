// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;

namespace AutomotiveSkill.Common
{
    /// <summary>
    /// Precomputed information about a bag of tokens.
    /// We purposely disregard the order of the tokens because we want e.g.,
    /// "left rear temperature" to match "rear left temperature".
    /// </summary>
    public class MatchableBagOfTokens
    {
        public string CanonicalSettingName { get; set; }

        public string CanonicalValueName { get; set; }

        public IList<string> Tokens { get; set; } = new List<string>();

        // The tokens_list contains a list of tokenized setting names
        // An element in the tokens_list is a set of tokens for a setting name
        public IList<IList<string>> TokensList { get; set; } = new List<IList<string>>();

        public bool IsEmpty()
        {
            return !Tokens.Any() && !TokensList.Any();
        }
    }
}