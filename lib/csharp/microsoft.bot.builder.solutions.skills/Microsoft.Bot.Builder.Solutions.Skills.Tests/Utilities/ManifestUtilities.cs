using System;
using System.Collections.Generic;
using Microsoft.Bot.Builder.Solutions.Skills.Models.Manifest;
using Action = Microsoft.Bot.Builder.Solutions.Skills.Models.Manifest.Action;

namespace Microsoft.Bot.Builder.Solutions.Skills.Tests.Utilities
{
    public static class ManifestUtilities
    {
        public static SkillManifest CreateSkill(string id, string name, string endpoint, string actionId, List<Slot> slots = null)
        {
            var skillManifest = new SkillManifest
            {
                Name = name,
                Id = id,
                Endpoint = new Uri(endpoint),
            };

            var action = new Action
            {
                Id = actionId,
                Definition = new ActionDefinition(),
            };

            // Provide slots if we have them
            if (slots != null)
            {
                action.Definition.Slots.AddRange(slots);
            }

            skillManifest.Actions.Add(action);

            return skillManifest;
        }

        public static Action CreateAction(string id, List<Slot> slots = null)
        {
            var action = new Action();

            action.Id = id;
            action.Definition = new ActionDefinition();
            action.Definition.Slots.AddRange(slots);

            return action;
        }
    }
}
