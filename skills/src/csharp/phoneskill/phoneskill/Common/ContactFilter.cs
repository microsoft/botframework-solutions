using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.AI.Luis;
using Microsoft.Graph;
using Microsoft.PhoneticMatching.Matchers.ContactMatcher;
using PhoneSkill.Models;
using PhoneSkill.Services.Luis;

namespace PhoneSkill.Common
{
    /// <summary>
    /// Business logic for filtering contacts.
    /// </summary>
    public class ContactFilter
    {
        private static readonly Regex MultipleWhitespaceRegex = new Regex(@"\s{2,}");

        /// <summary>
        /// Filters the user's contact list repeatedly based on the user's input to determine the right contact and phone number to call.
        /// </summary>
        /// <param name="state">The current conversation state. This will be modified.</param>
        /// <param name="contactProvider">The provider for the user's contact list. This may be null if the contact list is not to be used.</param>
        /// <returns>The first boolean indicates whether filtering was actually performed. (In some cases, no filtering is necessary.)
        /// The second boolean indicates whether any of the contacts has a phone number whose type matches the requested type.</returns>
        public async Task<(bool, bool)> Filter(PhoneSkillState state, IContactProvider contactProvider)
        {
            var isFiltered = false;

            var searchQuery = string.Empty;
            if (state.LuisResult.Entities.contactName != null)
            {
                searchQuery = string.Join(" ", state.LuisResult.Entities.contactName);
            }

            if (searchQuery.Any() && !(searchQuery == state.ContactResult.SearchQuery && state.ContactResult.Matches.Any()))
            {
                IList<ContactCandidate> contacts;
                if (state.ContactResult.Matches.Any())
                {
                    contacts = state.ContactResult.Matches;
                }
                else if (contactProvider != null)
                {
                    contacts = await contactProvider.GetContacts();
                }
                else
                {
                    contacts = new List<ContactCandidate>();
                }

                if (contacts.Any())
                {
                    // TODO Adjust max number of returned contacts?
                    var matcher = new EnContactMatcher<ContactCandidate>(contacts, ExtractContactFields);
                    var matches = matcher.FindByName(searchQuery);

                    if (!state.ContactResult.Matches.Any() || matches.Any())
                    {
                        isFiltered = isFiltered || matches.Count != state.ContactResult.Matches.Count;
                        state.ContactResult.SearchQuery = searchQuery;
                        state.ContactResult.Matches = matches;
                    }
                }
            }

            SetRequestedPhoneNumberType(state);
            var hasPhoneNumberOfRequestedType = false;
            (isFiltered, hasPhoneNumberOfRequestedType) = FilterPhoneNumbersByType(state, isFiltered);

            SetPhoneNumber(state);

            return (isFiltered, hasPhoneNumberOfRequestedType);
        }

        /// <summary>
        /// Returns whether a recipient has been specified.
        /// </summary>
        /// <param name="state">The current state.</param>
        /// <returns>Whether a recipient has been specified.</returns>
        public bool HasRecipient(PhoneSkillState state)
        {
            return state.ContactResult.Matches.Any() || state.PhoneNumber.Any();
        }

        /// <summary>
        /// Returns whether the contact has been completely disambiguated.
        /// </summary>
        /// <param name="state">The current state.</param>
        /// <returns>Whether the contact has been completely disambiguated.</returns>
        public bool IsContactDisambiguated(PhoneSkillState state)
        {
            return state.ContactResult.Matches.Count == 1 || state.PhoneNumber.Any();
        }

        /// <summary>
        /// Returns whether the contact's phone number has been completely disambiguated.
        /// </summary>
        /// <param name="state">The current state.</param>
        /// <returns>Whether the contact's phone number has been completely disambiguated.</returns>
        public bool IsPhoneNumberDisambiguated(PhoneSkillState state)
        {
            return (state.ContactResult.Matches.Count == 1 && state.ContactResult.Matches[0].PhoneNumbers.Count == 1) || state.PhoneNumber.Any();
        }

        /// <summary>
        /// Override the entities on the state with the ones from the given LUIS result.
        /// </summary>
        /// <param name="state">The current state. This will be modified.</param>
        /// <param name="phoneResult">The LUIS result.</param>
        public void OverrideEntities(PhoneSkillState state, PhoneLuis phoneResult)
        {
            state.LuisResult.Entities.contactName = phoneResult.Entities.contactName;
            state.LuisResult.Entities._instance.contactName = phoneResult.Entities._instance.contactName;

            state.LuisResult.Entities.phoneNumber = phoneResult.Entities.phoneNumber;
            state.LuisResult.Entities._instance.phoneNumber = phoneResult.Entities._instance.phoneNumber;

            state.LuisResult.Entities.phoneNumberSpelledOut = phoneResult.Entities.phoneNumberSpelledOut;
            state.LuisResult.Entities._instance.phoneNumberSpelledOut = phoneResult.Entities._instance.phoneNumberSpelledOut;

            if (state.LuisResult.Entities.phoneNumberType == null
                || !state.LuisResult.Entities.phoneNumberType.Any()
                || (phoneResult.Entities.phoneNumberType != null && phoneResult.Entities.phoneNumberType.Any()))
            {
                state.LuisResult.Entities.phoneNumberType = phoneResult.Entities.phoneNumberType;
                state.LuisResult.Entities._instance.phoneNumberType = phoneResult.Entities._instance.phoneNumberType;
            }
        }

        /// <summary>
        /// Override the entities on the state with the ones from the given LUIS result.
        /// </summary>
        /// <param name="state">The current state. This will be modified.</param>
        /// <param name="contactSelectionResult">The LUIS result.</param>
        public void OverrideEntities(PhoneSkillState state, ContactSelectionLuis contactSelectionResult)
        {
            state.LuisResult.Entities.contactName = contactSelectionResult.Entities.contactName;
            state.LuisResult.Entities._instance.contactName = contactSelectionResult.Entities._instance.contactName;

            state.LuisResult.Entities.phoneNumber = new string[0];
            state.LuisResult.Entities._instance.phoneNumber = new InstanceData[0];

            state.LuisResult.Entities.phoneNumberSpelledOut = new string[0];
            state.LuisResult.Entities._instance.phoneNumberSpelledOut = new InstanceData[0];
        }

        /// <summary>
        /// Override the entities on the state with the ones from the given LUIS result.
        /// </summary>
        /// <param name="state">The current state. This will be modified.</param>
        /// <param name="phoneNumberSelectionResult">The LUIS result.</param>
        public void OverrideEntities(PhoneSkillState state, PhoneNumberSelectionLuis phoneNumberSelectionResult)
        {
            state.LuisResult.Entities.phoneNumber = new string[0];
            state.LuisResult.Entities._instance.phoneNumber = new InstanceData[0];

            state.LuisResult.Entities.phoneNumberSpelledOut = new string[0];
            state.LuisResult.Entities._instance.phoneNumberSpelledOut = new InstanceData[0];

            if (state.LuisResult.Entities.phoneNumberType == null
                || !state.LuisResult.Entities.phoneNumberType.Any()
                || (phoneNumberSelectionResult.Entities.phoneNumberType != null && phoneNumberSelectionResult.Entities.phoneNumberType.Any()))
            {
                state.LuisResult.Entities.phoneNumberType = phoneNumberSelectionResult.Entities.phoneNumberType;
                state.LuisResult.Entities._instance.phoneNumberType = phoneNumberSelectionResult.Entities._instance.phoneNumberType;
            }
        }

        /// <summary>
        /// Remove and return contact candidates that don't have any phone number.
        /// </summary>
        /// <param name="state">The current state. This will be modified.</param>
        /// <returns>The removed candidates.</returns>
        public List<ContactCandidate> RemoveContactsWithNoPhoneNumber(PhoneSkillState state)
        {
            var newCandidates = new List<ContactCandidate>();
            var removedCandidates = new List<ContactCandidate>();
            foreach (var candidate in state.ContactResult.Matches)
            {
                if (candidate.PhoneNumbers.Any())
                {
                    newCandidates.Add(candidate);
                }
                else
                {
                    removedCandidates.Add(candidate);
                }
            }

            if (newCandidates.Count != state.ContactResult.Matches.Count)
            {
                state.ContactResult.Matches = newCandidates;
            }

            return removedCandidates;
        }

        /// <summary>
        /// Pre-process the given string by normalizing unicode characters, trimming whitespace, and normalizing whitespace.
        /// </summary>
        /// <param name="raw">The string to pre-process.</param>
        /// <returns>A pre-processed copy of the string.</returns>
        public string PreProcess(string raw)
        {
            var preprocessed = raw.Normalize(NormalizationForm.FormKC);
            preprocessed = MultipleWhitespaceRegex.Replace(preprocessed.Trim(), " ");
            return preprocessed;
        }

        private ContactFields ExtractContactFields(ContactCandidate contact)
        {
            return new ContactFields
            {
                Name = contact.Name,
            };
        }

        private void SetRequestedPhoneNumberType(PhoneSkillState state)
        {
            if (!state.ContactResult.RequestedPhoneNumberType.Any())
            {
                var entities = state.LuisResult.Entities;
                if (entities.phoneNumberType != null
                    && entities.phoneNumberType.Length > 0
                    && entities._instance.phoneNumberType != null
                    && entities._instance.phoneNumberType.Length > 0)
                {
                    var instanceData = entities._instance.phoneNumberType[0];
                    var resolvedValues = entities.phoneNumberType[0];
                    foreach (var value in resolvedValues)
                    {
                        state.ContactResult.RequestedPhoneNumberType = ParsePhoneNumberType(value, instanceData);
                        break;
                    }
                }
            }
        }

        private PhoneNumberType ParsePhoneNumberType(string resolvedValue, InstanceData entity)
        {
            var type = new PhoneNumberType
            {
                FreeForm = entity.Text,
            };

            if (Enum.TryParse(resolvedValue, out PhoneNumberType.StandardType standardType))
            {
                type.Standardized = standardType;
            }

            return type;
        }

        /// <summary>
        /// If there is a requested phone number type, filter the phone numbers of each contact candidate by that type.
        /// Exception: if this would remove all phone numbers of a candidate, keep them all, so the user can choose a different one.
        /// </summary>
        /// <param name="state">The current state. This will be modified.</param>
        /// <param name="isFiltered">Whether filtering was actually performed before this method was called.</param>
        /// <returns>Whether filtering was actually performed, either by this method or before.</returns>
        private (bool, bool) FilterPhoneNumbersByType(PhoneSkillState state, bool isFiltered)
        {
            var hasPhoneNumberOfRequestedType = false;
            for (int i = 0; i < state.ContactResult.Matches.Count; ++i)
            {
                var candidate = state.ContactResult.Matches[i];
                if (state.ContactResult.RequestedPhoneNumberType.Any())
                {
                    var phoneNumbersOfCorrectType = new List<PhoneNumber>();
                    foreach (var phoneNumber in candidate.PhoneNumbers)
                    {
                        if (DoPhoneNumberTypesMatch(state.ContactResult.RequestedPhoneNumberType, phoneNumber.Type))
                        {
                            phoneNumbersOfCorrectType.Add(phoneNumber);
                            hasPhoneNumberOfRequestedType = true;
                        }
                    }

                    if (phoneNumbersOfCorrectType.Any())
                    {
                        isFiltered = isFiltered || phoneNumbersOfCorrectType.Count != candidate.PhoneNumbers.Count;
                        candidate = (ContactCandidate)candidate.Clone();
                        candidate.PhoneNumbers = phoneNumbersOfCorrectType;
                        state.ContactResult.Matches[i] = candidate;
                    }
                }
            }

            return (isFiltered, hasPhoneNumberOfRequestedType);
        }

        private bool DoPhoneNumberTypesMatch(PhoneNumberType requestedType, PhoneNumberType actualType)
        {
            if (requestedType.Standardized != PhoneNumberType.StandardType.NONE)
            {
                return requestedType.Standardized == actualType.Standardized;
            }

            return PreProcess(requestedType.FreeForm).Equals(PreProcess(actualType.FreeForm), StringComparison.OrdinalIgnoreCase);
        }

        private void SetPhoneNumber(PhoneSkillState state)
        {
            if (!state.PhoneNumber.Any())
            {
                var entities = state.LuisResult.Entities;
                if (state.ContactResult.Matches.Count == 1 && state.ContactResult.Matches[0].PhoneNumbers.Count == 1)
                {
                    state.PhoneNumber = state.ContactResult.Matches[0].PhoneNumbers[0].Number;
                }
                else if (entities.phoneNumber != null && entities.phoneNumber.Any())
                {
                    state.PhoneNumber = string.Join(" ", entities.phoneNumber);
                }
            }
        }
    }
}
