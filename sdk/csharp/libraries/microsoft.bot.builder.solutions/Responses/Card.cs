// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace Microsoft.Bot.Builder.Solutions.Responses
{
    [Obsolete("This type is being deprecated. It's moved to the assembly Microsoft.Bot.Solutions. Please refer to https://aka.ms/botframework-solutions/releases/0_8", false)]
    public class Card
    {
        public Card()
        {
        }

        public Card(string name)
        {
            Name = name;
        }

        public Card(string name, ICardData data)
        {
            Name = name;
            Data = data;
        }

        public string Name { get; set; }

        public ICardData Data { get; set; }
    }
}