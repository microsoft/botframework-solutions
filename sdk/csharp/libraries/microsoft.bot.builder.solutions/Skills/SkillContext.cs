// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace Microsoft.Bot.Builder.Solutions.Skills
{
    /// <summary>
    ///  Context to share state between Bots and Skills.
    /// </summary>
    [Obsolete("This type is being deprecated. To continue using Skill capability please refer to https://aka.ms/botframework-solutions/releases/0_8", false)]
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