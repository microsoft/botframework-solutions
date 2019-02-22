using System.Collections.Generic;
using System.Threading.Tasks;
using PhoneSkill.Common;
using PhoneSkill.Models;

namespace PhoneSkillTest.TestDouble
{
    public class StubContactProvider : IContactProvider
    {
        public static readonly ContactCandidate AndrewSmith = new ContactCandidate
        {
            Name = "Andrew Smith",
            PhoneNumbers = new List<PhoneNumber>
            {
                new PhoneNumber
                {
                    Number = "555 111 1111",
                    Type = new PhoneNumberType
                    {
                        Standardized = PhoneNumberType.StandardType.HOME,
                    },
                },
                new PhoneNumber
                {
                    Number = "555 222 2222",
                    Type = new PhoneNumberType
                    {
                        Standardized = PhoneNumberType.StandardType.BUSINESS,
                    },
                },
                new PhoneNumber
                {
                    Number = "555 333 3333",
                    Type = new PhoneNumberType
                    {
                        Standardized = PhoneNumberType.StandardType.MOBILE,
                    },
                },
            },
        };

        public static readonly ContactCandidate AndrewJohnFitzroy = new ContactCandidate
        {
            Name = "Andrew John Fitzroy",
            PhoneNumbers = new List<PhoneNumber>
            {
                new PhoneNumber
                {
                    Number = "555 444 4444",
                    Type = new PhoneNumberType
                    {
                        Standardized = PhoneNumberType.StandardType.BUSINESS,
                    },
                },
                new PhoneNumber
                {
                    Number = "555 555 5555",
                    Type = new PhoneNumberType
                    {
                        Standardized = PhoneNumberType.StandardType.MOBILE,
                    },
                },
            },
        };

        public static readonly ContactCandidate AndrewJohnKeith = new ContactCandidate
        {
            Name = "Andrew John Keith",
            PhoneNumbers = new List<PhoneNumber>
            {
                new PhoneNumber
                {
                    Number = "555 444 5555",
                    Type = new PhoneNumberType
                    {
                        Standardized = PhoneNumberType.StandardType.BUSINESS,
                    },
                },
            },
        };

        public static readonly ContactCandidate BobBotter = new ContactCandidate
        {
            Name = "Bob Botter",
            PhoneNumbers = new List<PhoneNumber>
            {
                new PhoneNumber
                {
                    Number = "555 666 6666",
                    Type = new PhoneNumberType
                    {
                        Standardized = PhoneNumberType.StandardType.HOME,
                    },
                },
            },
        };

        public static readonly ContactCandidate ChristinaBotter = new ContactCandidate
        {
            Name = "Christina Botter",
        };

        public static readonly ContactCandidate ChristinaSanchez = new ContactCandidate
        {
            Name = "Christina Sanchez",
        };

        public static readonly ContactCandidate DithaNarthwani = new ContactCandidate
        {
            Name = "Ditha Narthwani",
            PhoneNumbers = new List<PhoneNumber>
            {
                new PhoneNumber
                {
                    Number = "555 777 7777",
                    Type = new PhoneNumberType
                    {
                        Standardized = PhoneNumberType.StandardType.MOBILE,
                    },
                },
            },
        };

        public static readonly ContactCandidate SanjayNarthwani = new ContactCandidate
        {
            Name = "Sanjay Narthwani",
            PhoneNumbers = new List<PhoneNumber>
            {
                new PhoneNumber
                {
                    Number = "555 888 8888",
                    Type = new PhoneNumberType
                    {
                        Standardized = PhoneNumberType.StandardType.MOBILE,
                    },
                },
            },
        };

        public static readonly ContactCandidate EveSmith = new ContactCandidate
        {
            Name = "Eve Smith",
            PhoneNumbers = new List<PhoneNumber>
            {
                new PhoneNumber
                {
                    Number = "555 999 9999",
                    Type = new PhoneNumberType
                    {
                        Standardized = PhoneNumberType.StandardType.HOME,
                    },
                },
                new PhoneNumber
                {
                    Number = "555 101 0101",
                    Type = new PhoneNumberType
                    {
                        Standardized = PhoneNumberType.StandardType.MOBILE,
                    },
                },
                new PhoneNumber
                {
                    Number = "555 121 2121",
                    Type = new PhoneNumberType
                    {
                        Standardized = PhoneNumberType.StandardType.MOBILE,
                    },
                },
            },
        };

        public static readonly ContactCandidate FernandaSanchez = new ContactCandidate
        {
            Name = "Fernanda Sanchez",
            PhoneNumbers = new List<PhoneNumber>
            {
                new PhoneNumber
                {
                    Number = "555 131 3131",
                    Type = new PhoneNumberType
                    {
                        Standardized = PhoneNumberType.StandardType.MOBILE,
                    },
                },
            },
        };

        public static readonly ContactCandidate GerardoSanchez = new ContactCandidate
        {
            Name = "Gerardo Sanchez",
            PhoneNumbers = new List<PhoneNumber>
            {
                new PhoneNumber
                {
                    Number = "555 141 4141",
                    Type = new PhoneNumberType
                    {
                        Standardized = PhoneNumberType.StandardType.MOBILE,
                    },
                },
            },
        };

        private readonly IList<ContactCandidate> contacts = new List<ContactCandidate>
        {
            AndrewSmith,
            AndrewJohnFitzroy,
            AndrewJohnKeith,
            BobBotter,
            ChristinaBotter,
            ChristinaSanchez,
            DithaNarthwani,
            SanjayNarthwani,
            EveSmith,
            FernandaSanchez,
            GerardoSanchez,
        };

        public Task<IList<ContactCandidate>> GetContacts()
        {
            return Task.FromResult<IList<ContactCandidate>>(contacts);
        }
    }
}
