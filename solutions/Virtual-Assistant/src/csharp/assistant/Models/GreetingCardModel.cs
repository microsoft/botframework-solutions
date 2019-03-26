using Microsoft.Bot.Builder.Solutions.Responses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace VirtualAssistant.Models
{
    public class GreetingCardModel : ICardData
    {
        public string HeaderUrl { get; set; }

        public string Title { get; set; }

        public string Body { get; set; }
    }
}
