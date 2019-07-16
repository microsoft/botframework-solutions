// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Luis;

namespace $safeprojectname$.Models
{
    public class SkillState
    {
        public string Token { get; set; }

        public $safeprojectname$Luis LuisResult { get; set; }

        public void Clear()
        {
        }
    }
}
