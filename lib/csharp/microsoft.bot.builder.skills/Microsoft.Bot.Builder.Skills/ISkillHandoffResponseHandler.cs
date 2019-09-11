// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Schema;

namespace Microsoft.Bot.Builder.Skills
{
    public interface ISkillHandoffResponseHandler
    {
        void HandleHandoffResponse(Activity activity);
    }
}
