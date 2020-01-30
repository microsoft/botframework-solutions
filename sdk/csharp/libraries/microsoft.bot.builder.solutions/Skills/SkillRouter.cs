// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Bot.Builder.Solutions.Skills.Models.Manifest;

namespace Microsoft.Bot.Builder.Solutions.Skills
{
    /// <summary>
    /// Skill Router class that helps Bots identify if a registered Skill matches the identified dispatch intent.
    /// </summary>
    [Obsolete("This type is being deprecated. To continue using Skill capability please refer to https://aka.ms/botframework-solutions/releases/0_8", false)]
    public static class SkillRouter
    {
        /// <summary>
        /// Helper method to go through a SkillManifeste and match the passed dispatch intent to a registered action.
        /// </summary>
        /// <param name="skillConfiguration">The Skill Configuration for the current Bot.</param>
        /// <param name="dispatchIntent">The Dispatch intent to try and match to a skill.</param>
        /// <returns>Whether the intent matches a Skill.</returns>
        public static SkillManifest IsSkill(List<SkillManifest> skillConfiguration, string dispatchIntent)
        {
            var manifest = skillConfiguration.SingleOrDefault(s => s.Actions.Any(a => a.Id == dispatchIntent.ToString()));

            if (manifest == null)
            {
                manifest = skillConfiguration.SingleOrDefault(s => s.Id == dispatchIntent.ToString());
            }

            return manifest;
        }
    }
}