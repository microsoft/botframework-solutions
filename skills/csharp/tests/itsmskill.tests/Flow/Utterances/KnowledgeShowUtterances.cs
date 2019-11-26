// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using ITSMSkill.Tests.API.Fakes;
using ITSMSkill.Tests.Flow.Strings;
using static Luis.ITSMLuis;

namespace ITSMSkill.Tests.Flow.Utterances
{
    public class KnowledgeShowUtterances : ITSMTestUtterances
    {
        public static readonly string Show = "search knowledgebase";

        public KnowledgeShowUtterances()
        {
            AddIntent(Show, Intent.KnowledgeShow);
        }
    }
}
