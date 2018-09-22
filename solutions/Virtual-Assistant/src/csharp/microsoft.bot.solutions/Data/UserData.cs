// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Bot.Solutions.Data
{
    using System;
    using System.Collections.Generic;
    using Microsoft.Bot.Schema;

    public enum EmailPerferenceOption
    {
        Default,
        MsGraph,
        Gmail,
    }

    public class UserData
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UserData"/> class.
        /// </summary>
        public UserData()
        {
        }

        /// <summary>
        /// Gets or sets.
        /// </summary>
        public string UserID
        {
            get;
            set;
        }

        public string Location
        {
            get;
            set;
        }

        public GeoCoordinates GeoLocation
        {
            get;
            set;
        }

        public TimeZoneInfo TimeZone
        {
            get;
            set;
        }

        public string Name
        {
            get;
            set;
        }

        public string PrimaryMail
        {
            get;
            set;
        }

        public string SecondaryMail
        {
            get;
            set;
        }

        public EmailPerferenceOption Option
        {
            get;
            set;
        }
    }
}
