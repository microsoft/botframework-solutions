// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Luis;

namespace $safeprojectname$.Models
{
    public class SkillState
    {
        public string Token { get; internal set; }

        public $safeprojectname$Luis LuisResult { get; internal set; }

        public void Clear()
        {
        }
    }
}
