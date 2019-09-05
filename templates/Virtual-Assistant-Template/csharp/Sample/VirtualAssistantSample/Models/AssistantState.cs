// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace VirtualAssistantSample.Models
{
    public class AssistantState
    {
        public string Name { get; set; }

        public string Email { get; set; }

        public string Location { get; set; }

        public Luis.GeneralLuis GeneralLuisResult { get; set; }
    }
}
