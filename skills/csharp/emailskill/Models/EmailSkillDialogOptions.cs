// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace EmailSkill.Models
{
    public class EmailSkillDialogOptions
    {
        public bool SubFlowMode { get; set; } = false;

        public bool IsAction { get; set; } = false;
    }
}