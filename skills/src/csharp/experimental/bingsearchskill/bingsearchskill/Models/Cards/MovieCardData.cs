using BingSearchSkill.Responses;
using Microsoft.Bot.Builder.Solutions.Responses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BingSearchSkill.Models.Cards
{
    public class MovieCardData : ICardData
    {
        public string Title { get; set; }

        public string Type { get; set; }

        public string Score { get; set; }

        public string Description { get; set; }

        public string Title_View { get; } = CommonStrings.View;

        public string Link_View { get; set; }

        public string Title_Trailers { get; } = CommonStrings.Trailers;

        public string Link_Trailers { get; set; }

        public string Title_Trivia { get; } = CommonStrings.Trivia;

        public string Link_Trivia { get; set; }

        public string IconPath { get; set; }
    }
}
