// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Bot.Solutions.Cards
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    public class BasicInfoData : CardDataBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BasicInfoData"/> class.
        /// </summary>
        public BasicInfoData()
        {
        }

        public string NameInfo
        {
            get;
            set;
        }

        public string LocationInfo
        {
            get;
            set;
        }

        public string PrimaryEmailInfo
        {
            get;
            set;
        }

        public string SecondaryEmailInfo
        {
            get;
            set;
        }
    }
}
