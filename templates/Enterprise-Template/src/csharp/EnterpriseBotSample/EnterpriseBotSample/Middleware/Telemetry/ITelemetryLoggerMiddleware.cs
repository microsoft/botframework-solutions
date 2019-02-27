// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;

namespace EnterpriseBotSample.Middleware.Telemetry
{
    public interface ITelemetryLoggerMiddleware
    {
        bool LogPersonalInformation { get; }

        /// <summary>
        /// Fills the Application Insights Custom Event properties for BotMessageReceived.
        /// These properties are logged in the custom event when a new message is received from the user.
        /// </summary>
        /// <param name="activity">Last activity sent from user.</param>
        /// <returns>A dictionary that is sent as "Properties" to Application Insights TrackEvent method for the BotMessageReceived Message.</returns>
        Dictionary<string, string> FillReceiveEventProperties(Activity activity);

        /// <summary>
        /// Fills the Application Insights Custom Event properties for BotMessageSend.
        /// These properties are logged in the custom event when a response message is sent by the Bot to the user.
        /// </summary>
        /// <param name="activity">Last activity sent from user.</param>
        /// <returns>A dictionary that is sent as "Properties" to Application Insights TrackEvent method for the BotMessageSend Message.</returns>
        Dictionary<string, string> FillSendEventProperties(Activity activity);

        /// <summary>
        /// Fills the Application Insights Custom Event properties for BotMessageUpdate.
        /// These properties are logged in the custom event when an activity message is updated by the Bot.
        /// For example, if a card is interacted with by the use, and the card needs to be updated to reflect
        /// some interaction.
        /// </summary>
        /// <param name="activity">Last activity sent from user.</param>
        /// <returns>A dictionary that is sent as "Properties" to Application Insights TrackEvent method for the BotMessageUpdate Message.</returns>
        Dictionary<string, string> FillUpdateEventProperties(Activity activity);

        /// <summary>
        /// Fills the Application Insights Custom Event properties for BotMessageDelete.
        /// These properties are logged in the custom event when an activity message is deleted by the Bot.  This is a relatively rare case.
        /// </summary>
        /// <param name="activity">Last activity sent from user.</param>
        /// <returns>A dictionary that is sent as "Properties" to Application Insights TrackEvent method for the BotMessageDelete Message.</returns>
        Dictionary<string, string> FillDeleteEventProperties(IMessageDeleteActivity activity);
    }
}