// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace EnterpriseBotSample.Middleware.Telemetry
{
    /// <summary>
    /// The Application Insights property names that we're logging.
    /// </summary>
    public static class TelemetryLoggerConstants
    {
        // Application Insights Custom Event name, logged when new message is received from the user
        public const string BotMsgReceiveEvent = "BotMessageReceived";

        // Application Insights Custom Event name, logged when a message is sent out from the bot
        public const string BotMsgSendEvent = "BotMessageSend";

        // Application Insights Custom Event name, logged when a message is updated by the bot (rare case)
        public const string BotMsgUpdateEvent = "BotMessageUpdate";

        // Application Insights Custom Event name, logged when a message is deleted by the bot (rare case)
        public const string BotMsgDeleteEvent = "BotMessageDelete";
    }
}