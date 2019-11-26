// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using Luis;
using Microsoft.Bot.Builder;
using static Luis.ITSMLuis;

namespace ITSMSkill.Tests.Flow.Utterances
{
    public class ITSMTestUtterances : BaseTestUtterances<ITSMLuis>
    {
        public override ITSMLuis NoneIntent { get; } = new ITSMLuis
        {
            Intents = new Dictionary<Intent, IntentScore>
            {
                { Intent.None, new IntentScore() { Score = TopIntentScore } }
            }
        };

        protected void AddIntent(
            string userInput,
            Intent intent,
            string[][] attributeType = null,
            string[][] ticketState = null,
            string[][] urgencyLevel = null,
            string[] ticketNumber = null,
            string[] closeReason = null,
            string[] ticketTitle = null)
        {
            var resultIntent = new ITSMLuis
            {
                Text = userInput,
                Intents = new Dictionary<Intent, IntentScore>
                {
                    { intent, new IntentScore() { Score = TopIntentScore } }
                }
            };

            resultIntent.Entities = new _Entities
            {
                AttributeType = attributeType,
                TicketState = ticketState,
                UrgencyLevel = urgencyLevel,
                TicketNumber = ticketNumber,
                CloseReason = closeReason,
                TicketTitle = ticketTitle
            };

            Add(userInput, resultIntent);
        }
    }
}
