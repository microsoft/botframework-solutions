// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BingSearchSkill.Responses;
using Microsoft.Bot.Solutions.Responses;

namespace BingSearchSkill.Models.Cards
{
    public class MovieCardData : ICardData
    {
        public string Name { get; set; }

        public string ContentRating { get; set; }

        public string Year { get; set; }

        public string GenreArray { get; set; }

        public string Duration { get; set; }

        public string Rating { get; set; }

        public string Description { get; set; }

        public string Image { get; set; }

        public string Speak { get; set; }
    }
}
