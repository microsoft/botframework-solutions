using Microsoft.Bot.Builder.Skills.Models.Manifest;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Bot.Builder.Skills.Tests.Utilities
{
    public static class ManifestUtilities
    {
        public static SkillManifest CreateSkill(string id, string name, string endpoint, string actionId, List<Models.Manifest.Slot> slots = null)
        {
            var skillManifest = new SkillManifest
            {
                Name = name,
                Id = id,
                Endpoint = new Uri(endpoint),
                Actions = new List<Models.Manifest.Action>()
            };

            var action = new Models.Manifest.Action
            {
                Id = actionId,
                Definition = new ActionDefinition()
            };

            // Provide slots if we have them
            if (slots != null)
            {              
                action.Definition.Slots = slots;
            }

            skillManifest.Actions.Add(action);

            return skillManifest;
        }
    }
}
