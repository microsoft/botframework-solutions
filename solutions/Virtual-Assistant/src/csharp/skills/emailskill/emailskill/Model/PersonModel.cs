// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;

namespace EmailSkill.Model
{
    /// <summary>
    /// Event mapping entity.
    /// </summary>
    public partial class PersonModel
    {
        /// <summary>
        /// The person source.
        /// </summary>
        private MailSource source;

        /// <summary>
        /// Initializes a new instance of the <see cref="PersonModel"/> class.、
        /// DO NOT USE THIS ONE.
        /// </summary>
        public PersonModel()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PersonModel"/> class from MS Graph person.
        /// </summary>
        /// <param name="msftPerson">MS Graph person.</param>
        public PersonModel(Microsoft.Graph.Person msftPerson)
        {
            source = MailSource.Microsoft;
            this.Id = msftPerson.Id;
            this.DisplayName = msftPerson?.DisplayName;
            this.UserPrincipalName = msftPerson?.UserPrincipalName;

            this.Emails = new List<string>();
            if (msftPerson?.ScoredEmailAddresses != null)
            {
                foreach (var email in msftPerson.ScoredEmailAddresses)
                {
                    this.Emails.Add(email.Address);
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PersonModel"/> class from Google person.
        /// </summary>
        /// <param name="gmailPerson">Google person.</param>
        public PersonModel(Google.Apis.People.v1.Data.Person gmailPerson)
        {
            source = MailSource.Google;
            this.Id = gmailPerson?.EmailAddresses?[0]?.Value;
            this.DisplayName = gmailPerson?.Names?[0]?.DisplayName;
            this.UserPrincipalName = gmailPerson?.Names?[0]?.DisplayNameLastFirst; ;

            this.Emails = new List<string>();
            if (gmailPerson?.EmailAddresses != null)
            {
                foreach (var email in gmailPerson.EmailAddresses)
                {
                    this.Emails.Add(email.Value);
                }
            }

            this.Photo = gmailPerson?.Photos?[0]?.Url;
        }

        public string Id {get; set;}

        public string DisplayName {get; set;}

        public string UserPrincipalName {get; set;}

        public List<string> Emails {get; set;}

        public string Photo {get; set;}

        public MailSource Source
        {
            get => source;

            set => source = value;
        }
    }
}
