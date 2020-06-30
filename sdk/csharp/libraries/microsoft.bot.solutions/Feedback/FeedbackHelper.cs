// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using Microsoft.Bot.Builder;

namespace Microsoft.Bot.Solutions.Feedback
{
    public class FeedbackHelper
    {
        private const string _traceName = "Feedback";

        public static void LogFeedback(FeedbackRecord record, IBotTelemetryClient botTelemetryClient)
        {
            var properties = new Dictionary<string, string>()
            {
                { nameof(FeedbackRecord.Tag), record.Tag },
                { nameof(FeedbackRecord.Feedback), record.Feedback },
                { nameof(FeedbackRecord.Comment), record.Comment },
                { nameof(FeedbackRecord.Request.Text), record.Request?.Text },
                { nameof(FeedbackRecord.Request.Id), record.Request?.Conversation.Id },
                { nameof(FeedbackRecord.Request.ChannelId), record.Request?.ChannelId },
            };

            botTelemetryClient.TrackEvent(_traceName, properties);
        }
    }
}
