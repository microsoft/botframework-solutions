// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AutomotiveSkill.Models
{
    /// <summary>
    /// Precomputed information about a bag of tokens.
    /// We purposely disregard the order of the tokens because we want e.g.,
    /// "left rear temperature" to match "rear left temperature".
    /// </summary>
    public class MatchableBagOfTokens
    {
        public string canonical_setting_name;
        public string canonical_value_name;
        public IList<string> tokens = new List<string>();
        // The tokens_list contains a list of tokenized setting names
        // An element in the tokens_list is a set of tokens for a setting name
        public IList<IList<string>> tokens_list = new List<IList<string>>();

        public bool IsEmpty()
        {
            return !tokens.Any() && !tokens_list.Any();
        }
    }

    public class ScoredMatchableBagOfTokens
    {
        public MatchableBagOfTokens option;
        public double score = 0.0;
    }

    public class MatchResult
    {
        public MatchableBagOfTokens element;
        public double score = 0.0;
    }

    /// <summary>
    /// A matching setting-value pair.
    /// </summary>
    public class SettingMatch
    {
        public string setting_name;
        public string value;
    }

    /// <summary>
    /// A setting value that can be selected from a list.
    /// </summary>
    public class SelectableSettingValue
    {
        /// <summary>
        /// The canonical name of the setting this value belongs to.
        /// </summary>
        public string canonicalSettingName;

        /// <summary>
        /// The setting value.
        /// </summary>
        public AvailableSettingValue value;
    }
}
