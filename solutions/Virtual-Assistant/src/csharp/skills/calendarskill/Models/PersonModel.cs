// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;

namespace CalendarSkill.Models
{
    /// <summary>
    /// Event mapping entity.
    /// </summary>
    public partial class PersonModel
    {
        /// <summary>
        /// The person source.
        /// </summary>
        private EventSource source;

        /// <summary>
        /// The person data of MS Graph.
        /// </summary>
        private Microsoft.Graph.Person msftPersonData;

        /// <summary>
        /// The person data of Google.
        /// </summary>
        private Google.Apis.People.v1.Data.Person gmailPersonData;

        /// <summary>
        /// Initializes a new instance of the <see cref="PersonModel"/> class.、
        /// DO NOT USE THIS ONE.
        /// </summary>
        public PersonModel()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PersonModel"/> class.
        /// </summary>
        /// <param name="source">the event source.</param>
        public PersonModel(EventSource source)
        {
            this.source = source;
            switch (this.source)
            {
                case EventSource.Microsoft:
                    msftPersonData = new Microsoft.Graph.Person();
                    break;
                case EventSource.Google:
                    gmailPersonData = new Google.Apis.People.v1.Data.Person();
                    break;
                default:
                    throw new Exception("Event Type not Defined");
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PersonModel"/> class from MS Graph person.
        /// </summary>
        /// <param name="msftPerson">MS Graph person.</param>
        public PersonModel(Microsoft.Graph.Person msftPerson)
        {
            source = EventSource.Microsoft;
            msftPersonData = msftPerson;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PersonModel"/> class from Google person.
        /// </summary>
        /// <param name="gmailPerson">Google person.</param>
        public PersonModel(Google.Apis.People.v1.Data.Person gmailPerson)
        {
            source = EventSource.Google;
            gmailPersonData = gmailPerson;
        }

        public dynamic Value
        {
            get
            {
                switch (source)
                {
                    case EventSource.Microsoft:
                        return msftPersonData;
                    case EventSource.Google:
                        return gmailPersonData;
                    case EventSource.Other:
                        return null;
                    default:
                        throw new Exception("Event Type not Defined");
                }
            }

            set
            {
                if (value is Google.Apis.People.v1.Data.Person)
                {
                    source = EventSource.Google;
                }

                if (value is Microsoft.Graph.Person)
                {
                    source = EventSource.Microsoft;
                }

                switch (source)
                {
                    case EventSource.Microsoft:
                        msftPersonData = value;
                        break;
                    case EventSource.Google:
                        gmailPersonData = value;
                        break;
                    case EventSource.Other:
                        throw new Exception("Get defaut type, please check");
                    default:
                        throw new Exception("Event Type not Defined");
                }
            }
        }

        public string GivenName
        {
            get
            {
                switch (source)
                {
                    case EventSource.Microsoft:
                        return msftPersonData.GivenName;
                    case EventSource.Google:
                        return gmailPersonData.Names[0]?.GivenName;
                    default:
                        throw new Exception("Event Type not Defined");
                }
            }

            set
            {
                switch (source)
                {
                    case EventSource.Microsoft:
                        msftPersonData.GivenName = value;
                        break;
                    case EventSource.Google:
                        if (gmailPersonData.Names == null)
                        {
                            gmailPersonData.Names = new List<Google.Apis.People.v1.Data.Name>();
                        }

                        if (gmailPersonData.Names.Count == 0)
                        {
                            Google.Apis.People.v1.Data.Name name = new Google.Apis.People.v1.Data.Name();
                            gmailPersonData.Names.Add(name);
                        }

                        gmailPersonData.Names[0].GivenName = value;
                        break;
                    default:
                        throw new Exception("Event Type not Defined");
                }
            }
        }

        public string Surname
        {
            get
            {
                switch (source)
                {
                    case EventSource.Microsoft:
                        return msftPersonData.Surname;
                    case EventSource.Google:
                        return gmailPersonData.Names[0]?.FamilyName;
                    default:
                        throw new Exception("Event Type not Defined");
                }
            }

            set
            {
                switch (source)
                {
                    case EventSource.Microsoft:
                        msftPersonData.Surname = value;
                        break;
                    case EventSource.Google:
                        if (gmailPersonData.Names == null)
                        {
                            gmailPersonData.Names = new List<Google.Apis.People.v1.Data.Name>();
                        }

                        if (gmailPersonData.Names.Count == 0)
                        {
                            Google.Apis.People.v1.Data.Name name = new Google.Apis.People.v1.Data.Name();
                            gmailPersonData.Names.Add(name);
                        }

                        gmailPersonData.Names[0].FamilyName = value;
                        break;
                    default:
                        throw new Exception("Event Type not Defined");
                }
            }
        }

        public string DisplayName
        {
            get
            {
                switch (source)
                {
                    case EventSource.Microsoft:
                        return msftPersonData.DisplayName;
                    case EventSource.Google:
                        return gmailPersonData.Names[0]?.DisplayName;
                    default:
                        throw new Exception("Event Type not Defined");
                }
            }

            set
            {
                switch (source)
                {
                    case EventSource.Microsoft:
                        msftPersonData.DisplayName = value;
                        break;
                    case EventSource.Google:
                        if (gmailPersonData.Names == null)
                        {
                            gmailPersonData.Names = new List<Google.Apis.People.v1.Data.Name>();
                        }

                        if (gmailPersonData.Names.Count == 0)
                        {
                            Google.Apis.People.v1.Data.Name name = new Google.Apis.People.v1.Data.Name();
                            gmailPersonData.Names.Add(name);
                        }

                        gmailPersonData.Names[0].DisplayName = value;
                        break;
                    default:
                        throw new Exception("Event Type not Defined");
                }
            }
        }

        public string UserPrincipalName
        {
            get
            {
                switch (source)
                {
                    case EventSource.Microsoft:
                        return msftPersonData.UserPrincipalName;
                    case EventSource.Google:
                        return gmailPersonData.Names[0]?.DisplayNameLastFirst;
                    default:
                        throw new Exception("Event Type not Defined");
                }
            }

            set
            {
                switch (source)
                {
                    case EventSource.Microsoft:
                        msftPersonData.UserPrincipalName = value;
                        break;
                    case EventSource.Google:
                        if (gmailPersonData.Names == null)
                        {
                            gmailPersonData.Names = new List<Google.Apis.People.v1.Data.Name>();
                        }

                        if (gmailPersonData.Names.Count == 0)
                        {
                            Google.Apis.People.v1.Data.Name name = new Google.Apis.People.v1.Data.Name();
                            gmailPersonData.Names.Add(name);
                        }

                        gmailPersonData.Names[0].DisplayNameLastFirst = value;
                        break;
                    default:
                        throw new Exception("Event Type not Defined");
                }
            }
        }

        public IList<string> Emails
        {
            get
            {
                IList<string> result = new List<string>();
                switch (source)
                {
                    case EventSource.Microsoft:
                        foreach (var email in msftPersonData.ScoredEmailAddresses)
                        {
                            result.Add(email.Address);
                        }

                        return result;
                    case EventSource.Google:
                        foreach (var email in gmailPersonData.EmailAddresses)
                        {
                            result.Add(email.Value);
                        }

                        return result;
                    default:
                        throw new Exception("Event Type not Defined");
                }
            }
        }

        public EventSource Source
        {
            get => source;

            set => source = value;
        }
    }
}
