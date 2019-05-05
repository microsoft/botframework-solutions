using BingSearchSkill.Responses;
using Microsoft.Bot.Builder.Solutions.Responses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BingSearchSkill.Models.Cards
{
    public class PersonCardData : ICardData
    {
        public string Name { get; set; }

        public string Description { get; set; }

        public string Title_View { get; } = CommonStrings.View;

        public string Link_View { get; set; }

        public string IconPath { get; set; }
    }
}
