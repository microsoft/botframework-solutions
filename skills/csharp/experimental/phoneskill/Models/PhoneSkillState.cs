// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using PhoneSkill.Services.Luis;

namespace PhoneSkill.Models
{
    public class PhoneSkillState : DialogState
    {
        public PhoneSkillState()
        {
            Clear();
        }

        /// <summary>
        /// Gets or sets the authentication token needed for getting the user's contact list.
        /// </summary>
        /// <value>
        /// The authentication token needed for getting the user's contact list.
        /// </value>
        public string Token { get; set; }

        /// <summary>
        /// Gets or sets the source of the user's contact list.
        /// </summary>
        /// <value>
        /// The source of the user's contact list.
        /// </value>
        public ContactSource? SourceOfContacts { get; set; }

        /// <summary>
        /// Gets or sets the most recent LUIS result.
        /// </summary>
        /// <value>
        /// The most recent LUIS result.
        /// </value>
        public PhoneLuis LuisResult { get; set; }

        /// <summary>
        /// Gets or sets the result of the contact search (if one was performed).
        /// </summary>
        /// <value>
        /// The result of the contact search (if one was performed).
        /// </value>
        public ContactSearchResult ContactResult { get; set; }

        /// <summary>
        /// Gets or sets the final phone number to call.
        /// </summary>
        /// <value>
        /// The final phone number to call.
        /// </value>
        public string PhoneNumber { get; set; }

        /// <summary>
        /// Clear the state.
        /// </summary>
        public void Clear()
        {
            Token = string.Empty;
            SourceOfContacts = null;
            ClearExceptAuth();
        }

        /// <summary>
        /// Clear the state except for authentication information.
        /// </summary>
        public void ClearExceptAuth()
        {
            LuisResult = new PhoneLuis()
            {
                Text = string.Empty,
                AlteredText = string.Empty,
                Intents = new Dictionary<PhoneLuis.Intent, IntentScore>(),
                Entities = new PhoneLuis._Entities()
                {
                    _instance = new PhoneLuis._Entities._Instance(),
                },
                Properties = new Dictionary<string, object>(),
            };

            ContactResult = new ContactSearchResult();
            PhoneNumber = string.Empty;
        }
    }
}
