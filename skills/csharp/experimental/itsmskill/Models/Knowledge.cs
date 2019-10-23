// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ITSMSkill.Models
{
    public class Knowledge
    {
        public string Id { get; set; }

        public string Title { get; set; }

        public DateTime UpdatedTime { get; set; }

        public string Content { get; set; }

        public string Number { get; set; }

        public string Url { get; set; }

        public string Provider { get; set; }
    }
}
