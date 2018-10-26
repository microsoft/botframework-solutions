// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using CustomerSupportTemplate.Models;
using Microsoft.Bot.Builder.Dialogs;

namespace CustomerSupportTemplate
{
    public class CustomerSupportTemplateState : DialogState
    {
        public bool IntroSent { get; set; }

        public Account Account { get; set; }

        public Order Order { get; set; }
    }
}
