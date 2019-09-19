// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ITSMSkill.Models
{
    public class KnowledgesResult : ResultBase
    {
        public Knowledge[] Knowledges { get; set; }
    }
}
