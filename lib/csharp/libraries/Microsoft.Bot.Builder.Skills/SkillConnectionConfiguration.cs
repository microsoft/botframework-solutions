// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Builder.Skills.Auth;

namespace Microsoft.Bot.Builder.Skills
{
    public class SkillConnectionConfiguration
    {
        public SkillOptions SkillOptions { get; set; }

        public IServiceClientCredentials ServiceClientCredentials { get; set; }
    }
}
