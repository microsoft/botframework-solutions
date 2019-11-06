// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace CalendarSkill.Models.DialogOptions
{
    public class CalendarSkillDialogOptions
    {
        public CalendarSkillDialogOptions()
        {
        }

        public CalendarSkillDialogOptions(object options)
        {
            var skillOptions = options as CalendarSkillDialogOptions;
            if (skillOptions != null)
            {
                SubFlowMode = skillOptions.SubFlowMode;
            }
        }

        public bool SubFlowMode { get; set; }
    }
}