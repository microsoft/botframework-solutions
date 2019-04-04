﻿// https://docs.microsoft.com/en-us/visualstudio/modeling/t4-include-directive?view=vs-2017
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Builder.Solutions.Responses;

namespace EmailSkill.Responses.DeleteEmail
{
    /// <summary>
    /// Contains bot responses.
    /// </summary>
    public class DeleteEmailResponses : IResponseIdCollection
    {
        // Generated accessors
        public const string DeletePrompt = "DeletePrompt";
        public const string DeleteConfirm = "DeleteConfirm";
        public const string DeleteSuccessfully = "DeleteSuccessfully";
    }
}