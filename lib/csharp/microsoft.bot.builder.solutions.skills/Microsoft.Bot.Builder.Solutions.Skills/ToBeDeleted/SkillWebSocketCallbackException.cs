// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace Microsoft.Bot.Builder.Solutions.Skills.ToBeDeleted
{
    public class SkillWebSocketCallbackException : Exception
    {
        public SkillWebSocketCallbackException()
            : base()
        {
        }

        public SkillWebSocketCallbackException(string message)
            : base(message)
        {
        }

        public SkillWebSocketCallbackException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
