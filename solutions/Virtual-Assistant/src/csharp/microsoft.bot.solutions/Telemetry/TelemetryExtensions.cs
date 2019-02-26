﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;

namespace Microsoft.Bot.Solutions.Telemetry
{
    /// <summary>
    /// This class hosts the extension methods for telemetry purposes to add the basic logging properties before taking the appropriate action.
    /// </summary>
    public static class TelemetryExtensions
    {
        public static void TrackEventEx(this IBotTelemetryClient telemetryClient, string eventName, Activity activity, string dialogId = null, IDictionary<string, string> properties = null, IDictionary<string, double> metrics = null)
        {
            telemetryClient.TrackEvent(eventName, GetFinalProperties(activity, dialogId, properties), metrics);
        }

        public static void TrackExceptionEx(this IBotTelemetryClient telemetryClient, Exception exception, Activity activity, string dialogId = null, IDictionary<string, string> properties = null, IDictionary<string, double> metrics = null)
        {
            telemetryClient.TrackException(exception, GetFinalProperties(activity, dialogId, properties), metrics);
        }

        public static void TrackTraceEx(this IBotTelemetryClient telemetryClient, string message, Severity severityLevel, Activity activity, IDictionary<string, string> properties, string dialogId = null)
        {
            telemetryClient.TrackTrace(message, severityLevel, GetFinalProperties(activity, dialogId, properties));
        }

        private static IDictionary<string, string> GetFinalProperties(Activity activity, string dialogId, IDictionary<string, string> properties = null)
        {
            var finalProperties = new Dictionary<string, string>();

            if (dialogId != null)
            {
                finalProperties.Add(TelemetryConstants.DialogIdProperty, dialogId);
            }

            if (properties != null && properties.Count > 0)
            {
                foreach (var property in properties)
                {
                    var key = property.Key;
                    var value = property.Value;

                    if (finalProperties.ContainsKey(key))
                    {
                        finalProperties[key] = value;
                    }
                    else
                    {
                        finalProperties.Add(key, value);
                    }
                }
            }

            return finalProperties;
        }
    }
}