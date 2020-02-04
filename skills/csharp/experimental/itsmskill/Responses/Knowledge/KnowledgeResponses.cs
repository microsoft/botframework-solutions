// https://docs.microsoft.com/en-us/visualstudio/modeling/t4-include-directive?view=vs-2017
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Solutions.Responses;

namespace ITSMSkill.Responses.Knowledge
{
    /// <summary>
    /// Contains bot responses.
    /// </summary>
    public class KnowledgeResponses : IResponseIdCollection
    {
        // Generated accessors
        public const string ShowExistingToSolve = "ShowExistingToSolve";
        public const string IfExistingSolve = "IfExistingSolve";
        public const string IfFindWanted = "IfFindWanted";
        public const string IfCreateTicket = "IfCreateTicket";
        public const string KnowledgeEnd = "KnowledgeEnd";
        public const string KnowledgeShowNone = "KnowledgeShowNone";
    }
}