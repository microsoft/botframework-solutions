// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ITSMSkill.Models
{
    public class ResultBase
    {
        public bool Success { get; set; }

        public string ErrorMessage { get; set; }
    }
}
