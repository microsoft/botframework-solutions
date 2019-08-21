// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace EventSkill.Models
{
    public class EventSkillUserState
    {
        public string Location { get; set; }

        public void Clear()
        {
            Location = null;
        }
    }
}
