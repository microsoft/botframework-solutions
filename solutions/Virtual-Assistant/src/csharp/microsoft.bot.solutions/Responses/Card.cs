using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Bot.Solutions.Responses
{
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
