﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace Microsoft.Bot.Builder.Solutions.Skills
{
    /// <summary>
    ///  Context to share state between Bots and Skills.
    /// </summary>
    public class SkillContext : Dictionary<string, JObject>
    {
        public SkillContext()
        {
        }

        public SkillContext(IDictionary<string, JObject> collection)
            : base(collection)
        {
        }
    }
}