// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Bot.Solutions.Skills.Models;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Bot.Solutions.Skills
{
    /// <summary>
    /// A helper class that loads Skills information from configuration.
    /// </summary>
    [ExcludeFromCodeCoverageAttribute]
    public class SkillsConfiguration
    {
        public SkillsConfiguration(IConfiguration configuration)
        {
            var section = configuration?.GetSection("BotFrameworkSkills");
            var skills = section?.Get<EnhancedBotFrameworkSkill[]>();
            if (skills != null)
            {
                foreach (var skill in skills)
                {
                    Skills.Add(skill.Id, skill);
                }
            }

            var skillHostEndpoint = configuration?.GetValue<string>(nameof(SkillHostEndpoint));
            if (!string.IsNullOrWhiteSpace(skillHostEndpoint))
            {
                SkillHostEndpoint = new Uri(skillHostEndpoint);
            }
        }

        public Uri SkillHostEndpoint { get; }

        public Dictionary<string, EnhancedBotFrameworkSkill> Skills { get; } = new Dictionary<string, EnhancedBotFrameworkSkill>();
    }
}
