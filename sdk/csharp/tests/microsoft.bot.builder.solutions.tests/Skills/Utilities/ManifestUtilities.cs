// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Microsoft.Bot.Builder.Solutions.Skills.Models.Manifest;

namespace Microsoft.Bot.Builder.Solutions.Tests.Skills.Utilities
{
    [Obsolete("This type is being deprecated.", false)]
    public static class ManifestUtilities
    {
        public static SkillManifest CreateSkill(string id, string name, string endpoint, string actionId, List<Slot> slots = null)
        {
            var skillManifest = new SkillManifest
            {
                Name = name,
                Id = id,
                Endpoint = new Uri(endpoint),
                Actions = new List<Solutions.Skills.Models.Manifest.Action>(),
            };

            var action = new Solutions.Skills.Models.Manifest.Action
            {
                Id = actionId,
                Definition = new ActionDefinition(),
            };

            // Provide slots if we have them
            if (slots != null)
            {
                action.Definition.Slots = slots;
            }

            skillManifest.Actions.Add(action);

            return skillManifest;
        }

        public static Solutions.Skills.Models.Manifest.Action CreateAction(string id, List<Slot> slots = null)
        {
            var action = new Solutions.Skills.Models.Manifest.Action();

            action.Id = id;
            action.Definition = new ActionDefinition();
            action.Definition.Slots = slots;

            return action;
        }
    }
}
