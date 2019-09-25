using System.Collections.Generic;
using Luis;
using Microsoft.Bot.Builder.Solutions.Testing.Mocks;
using PhoneSkill.Services.Luis;
using PhoneSkillTest.Flow.Utterances;

namespace PhoneSkillTest.TestDouble
{
    public class PhoneSkillMockLuisRecognizerFactory
    {
        public static MockLuisRecognizer CreateMockGeneralLuisRecognizer()
        {
            var builder = new MockLuisRecognizerBuilder<General, General.Intent>();

            builder.AddUtterance(GeneralUtterances.Cancel, General.Intent.Cancel);
            builder.AddUtterance(GeneralUtterances.Escalate, General.Intent.Escalate);
            builder.AddUtterance(GeneralUtterances.Help, General.Intent.Help);
            builder.AddUtterance(GeneralUtterances.Incomprehensible, General.Intent.None);
            builder.AddUtterance(GeneralUtterances.Logout, General.Intent.Logout);

            return builder.Build();
        }

        public static MockLuisRecognizer CreateMockPhoneLuisRecognizer()
        {
            var builder = new MockLuisRecognizerBuilder<PhoneLuis, PhoneLuis.Intent>();

            builder.AddUtterance(GeneralUtterances.Incomprehensible, PhoneLuis.Intent.None);

            builder.AddUtterance(OutgoingCallUtterances.OutgoingCallContactName, PhoneLuis.Intent.OutgoingCall, new List<MockLuisEntity>()
            {
                new MockLuisEntity
                {
                    Type = "contactName",
                    Text = "bob",
                    StartIndex = 5,
                },
            });

            builder.AddUtterance(OutgoingCallUtterances.OutgoingCallContactNameMultipleMatchesAndrew, PhoneLuis.Intent.OutgoingCall, new List<MockLuisEntity>()
            {
                new MockLuisEntity
                {
                    Type = "contactName",
                    Text = "andrew",
                    StartIndex = 5,
                },
            });

            builder.AddUtterance(OutgoingCallUtterances.OutgoingCallContactNameMultipleMatchesBotter, PhoneLuis.Intent.OutgoingCall, new List<MockLuisEntity>()
            {
                new MockLuisEntity
                {
                    Type = "contactName",
                    Text = "botter",
                    StartIndex = 5,
                },
            });

            builder.AddUtterance(OutgoingCallUtterances.OutgoingCallContactNameMultipleMatchesNarthwani, PhoneLuis.Intent.OutgoingCall, new List<MockLuisEntity>()
            {
                new MockLuisEntity
                {
                    Type = "contactName",
                    Text = "narthwani",
                    StartIndex = 5,
                },
            });

            builder.AddUtterance(OutgoingCallUtterances.OutgoingCallContactNameMultipleMatchesSanchez, PhoneLuis.Intent.OutgoingCall, new List<MockLuisEntity>()
            {
                new MockLuisEntity
                {
                    Type = "contactName",
                    Text = "sanchez",
                    StartIndex = 5,
                },
            });

            builder.AddUtterance(OutgoingCallUtterances.OutgoingCallContactNameMultipleMatchesWithSpeechRecognitionError, PhoneLuis.Intent.OutgoingCall, new List<MockLuisEntity>()
            {
                new MockLuisEntity
                {
                    Type = "contactName",
                    Text = "not funny",
                    StartIndex = 5,
                },
            });

            builder.AddUtterance(OutgoingCallUtterances.OutgoingCallContactNameMultipleNumbers, PhoneLuis.Intent.OutgoingCall, new List<MockLuisEntity>()
            {
                new MockLuisEntity
                {
                    Type = "contactName",
                    Text = "andrew smith",
                    StartIndex = 5,
                },
            });

            builder.AddUtterance(OutgoingCallUtterances.OutgoingCallContactNameMultipleNumbersWithSameType, PhoneLuis.Intent.OutgoingCall, new List<MockLuisEntity>()
            {
                new MockLuisEntity
                {
                    Type = "contactName",
                    Text = "eve smith",
                    StartIndex = 5,
                },
            });

            builder.AddUtterance(OutgoingCallUtterances.OutgoingCallContactNameNoPhoneNumber, PhoneLuis.Intent.OutgoingCall, new List<MockLuisEntity>()
            {
                new MockLuisEntity
                {
                    Type = "contactName",
                    Text = "christina botter",
                    StartIndex = 5,
                },
            });

            builder.AddUtterance(OutgoingCallUtterances.OutgoingCallContactNameNoPhoneNumberMultipleMatches, PhoneLuis.Intent.OutgoingCall, new List<MockLuisEntity>()
            {
                new MockLuisEntity
                {
                    Type = "contactName",
                    Text = "christina",
                    StartIndex = 5,
                },
            });

            builder.AddUtterance(OutgoingCallUtterances.OutgoingCallContactNameNotFound, PhoneLuis.Intent.OutgoingCall, new List<MockLuisEntity>()
            {
                new MockLuisEntity
                {
                    Type = "contactName",
                    Text = "qqq",
                    StartIndex = 5,
                },
            });

            builder.AddUtterance(OutgoingCallUtterances.OutgoingCallContactNameWithPhoneNumberType, PhoneLuis.Intent.OutgoingCall, new List<MockLuisEntity>()
            {
                new MockLuisEntity
                {
                    Type = "contactName",
                    Text = "andrew smith",
                    StartIndex = 5,
                },
                new MockLuisEntity
                {
                    Type = "phoneNumberType",
                    Text = "work",
                    StartIndex = 21,
                    ResolvedValue = "BUSINESS",
                },
            });

            builder.AddUtterance(OutgoingCallUtterances.OutgoingCallContactNameWithPhoneNumberTypeMultipleMatches, PhoneLuis.Intent.OutgoingCall, new List<MockLuisEntity>()
            {
                new MockLuisEntity
                {
                    Type = "contactName",
                    Text = "narthwani",
                    StartIndex = 5,
                },
                new MockLuisEntity
                {
                    Type = "phoneNumberType",
                    Text = "home",
                    StartIndex = 18,
                    ResolvedValue = "HOME",
                },
            });

            builder.AddUtterance(OutgoingCallUtterances.OutgoingCallContactNameWithPhoneNumberTypeNotFoundMultipleAlternatives, PhoneLuis.Intent.OutgoingCall, new List<MockLuisEntity>()
            {
                new MockLuisEntity
                {
                    Type = "contactName",
                    Text = "eve smith",
                    StartIndex = 5,
                },
                new MockLuisEntity
                {
                    Type = "phoneNumberType",
                    Text = "work",
                    StartIndex = 18,
                    ResolvedValue = "BUSINESS",
                },
            });

            builder.AddUtterance(OutgoingCallUtterances.OutgoingCallContactNameWithPhoneNumberTypeNotFoundSingleAlternative, PhoneLuis.Intent.OutgoingCall, new List<MockLuisEntity>()
            {
                new MockLuisEntity
                {
                    Type = "contactName",
                    Text = "bob botter",
                    StartIndex = 5,
                },
                new MockLuisEntity
                {
                    Type = "phoneNumberType",
                    Text = "mobile",
                    StartIndex = 19,
                    ResolvedValue = "MOBILE",
                },
            });

            builder.AddUtterance(OutgoingCallUtterances.OutgoingCallContactNameWithSpeechRecognitionError, PhoneLuis.Intent.OutgoingCall, new List<MockLuisEntity>()
            {
                new MockLuisEntity
                {
                    Type = "contactName",
                    Text = "sunday not funny",
                    StartIndex = 5,
                },
            });

            builder.AddUtterance(OutgoingCallUtterances.OutgoingCallNoEntities, PhoneLuis.Intent.OutgoingCall);

            builder.AddUtterance(OutgoingCallUtterances.OutgoingCallPhoneNumber, PhoneLuis.Intent.OutgoingCall, new List<MockLuisEntity>()
            {
                new MockLuisEntity
                {
                    Type = "phoneNumber",
                    Text = "0118 999 88199 9119 725 3",
                    StartIndex = 5,
                },
            });

            builder.AddUtterance(OutgoingCallUtterances.RecipientContactName, PhoneLuis.Intent.OutgoingCall, new List<MockLuisEntity>()
            {
                new MockLuisEntity
                {
                    Type = "contactName",
                    Text = "bob",
                    StartIndex = 0,
                },
            });

            builder.AddUtterance(OutgoingCallUtterances.RecipientContactNameWithPhoneNumberType, PhoneLuis.Intent.OutgoingCall, new List<MockLuisEntity>()
            {
                new MockLuisEntity
                {
                    Type = "contactName",
                    Text = "andrew smith",
                    StartIndex = 0,
                },
                new MockLuisEntity
                {
                    Type = "phoneNumberType",
                    Text = "work",
                    StartIndex = 16,
                    ResolvedValue = "BUSINESS",
                },
            });

            builder.AddUtterance(OutgoingCallUtterances.RecipientContactNameWithSpeechRecognitionError, PhoneLuis.Intent.OutgoingCall, new List<MockLuisEntity>()
            {
                new MockLuisEntity
                {
                    Type = "contactName",
                    Text = "sunday not funny",
                    StartIndex = 0,
                },
            });

            builder.AddUtterance(OutgoingCallUtterances.RecipientPhoneNumber, PhoneLuis.Intent.OutgoingCall, new List<MockLuisEntity>()
            {
                new MockLuisEntity
                {
                    Type = "phoneNumber",
                    Text = "0118 999 88199 9119 725 3",
                    StartIndex = 0,
                },
            });

            return builder.Build();
        }

        public static MockLuisRecognizer CreateMockContactSelectionLuisRecognizer()
        {
            var builder = new MockLuisRecognizerBuilder<ContactSelectionLuis, ContactSelectionLuis.Intent>();

            builder.AddUtterance(OutgoingCallUtterances.ContactSelectionFullName, ContactSelectionLuis.Intent.ContactSelection, new List<MockLuisEntity>()
            {
                new MockLuisEntity
                {
                    Type = "contactName",
                    Text = "sanjay narthwani",
                    StartIndex = 7,
                },
            });

            builder.AddUtterance(OutgoingCallUtterances.ContactSelectionFullNameWithSpeechRecognitionError, ContactSelectionLuis.Intent.ContactSelection, new List<MockLuisEntity>()
            {
                new MockLuisEntity
                {
                    Type = "contactName",
                    Text = "sunday not funny",
                    StartIndex = 7,
                },
            });

            builder.AddUtterance(OutgoingCallUtterances.ContactSelectionNoMatches, ContactSelectionLuis.Intent.ContactSelection, new List<MockLuisEntity>()
            {
                new MockLuisEntity
                {
                    Type = "contactName",
                    Text = "qqq",
                    StartIndex = 0,
                },
            });

            builder.AddUtterance(OutgoingCallUtterances.ContactSelectionPartialNameKeith, ContactSelectionLuis.Intent.ContactSelection, new List<MockLuisEntity>()
            {
                new MockLuisEntity
                {
                    Type = "contactName",
                    Text = "keith",
                    StartIndex = 7,
                },
            });

            builder.AddUtterance(OutgoingCallUtterances.ContactSelectionPartialNameAndrewJohn, ContactSelectionLuis.Intent.ContactSelection, new List<MockLuisEntity>()
            {
                new MockLuisEntity
                {
                    Type = "contactName",
                    Text = "andrew john",
                    StartIndex = 0,
                },
            });

            builder.AddUtterance(OutgoingCallUtterances.ContactSelectionPartialNameSanjay, ContactSelectionLuis.Intent.ContactSelection, new List<MockLuisEntity>()
            {
                new MockLuisEntity
                {
                    Type = "contactName",
                    Text = "sanjay",
                    StartIndex = 7,
                },
            });

            builder.AddUtterance(OutgoingCallUtterances.SelectionFirst, ContactSelectionLuis.Intent.ContactSelection);

            builder.AddUtterance(OutgoingCallUtterances.SelectionNoEntities, ContactSelectionLuis.Intent.ContactSelection);

            return builder.Build();
        }

        public static MockLuisRecognizer CreateMockPhoneNumberSelectionLuisRecognizer()
        {
            var builder = new MockLuisRecognizerBuilder<PhoneNumberSelectionLuis, PhoneNumberSelectionLuis.Intent>();

            builder.AddUtterance(OutgoingCallUtterances.PhoneNumberSelectionFullNumber, PhoneNumberSelectionLuis.Intent.PhoneNumberSelection);

            builder.AddUtterance(OutgoingCallUtterances.PhoneNumberSelectionNoMatches, PhoneNumberSelectionLuis.Intent.PhoneNumberSelection, new List<MockLuisEntity>()
            {
                new MockLuisEntity
                {
                    // TODO Change entity type once we support custom phone number types.
                    Type = "phoneNumberType",
                    Text = "fax",
                    StartIndex = 4,
                },
            });

            builder.AddUtterance(OutgoingCallUtterances.PhoneNumberSelectionStandardizedType, PhoneNumberSelectionLuis.Intent.PhoneNumberSelection, new List<MockLuisEntity>()
            {
                new MockLuisEntity
                {
                    Type = "phoneNumberType",
                    Text = "cell phone",
                    StartIndex = 9,
                    ResolvedValue = "MOBILE",
                },
            });

            builder.AddUtterance(OutgoingCallUtterances.SelectionFirst, PhoneNumberSelectionLuis.Intent.PhoneNumberSelection);

            builder.AddUtterance(OutgoingCallUtterances.SelectionNoEntities, PhoneNumberSelectionLuis.Intent.PhoneNumberSelection);

            return builder.Build();
        }
    }
}
