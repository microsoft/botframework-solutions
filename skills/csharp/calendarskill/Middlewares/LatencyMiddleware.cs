// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.AI.Luis;
using Microsoft.Bot.Builder.Solutions;
using Microsoft.Bot.Schema;
using Newtonsoft.Json.Linq;

namespace CalendarSkill.Middlewares
{
    public class LatencyMiddleware : IMiddleware
    {
        // Before name, for simplicity
        public static readonly string LatencyLabelKey = "LatencyLabelKey";
        public static readonly string LatencyParentKey = "LatencyParentKey";
        public static readonly string LatencyTurnNameKey = "LatencyTurnNameKey";
        // Names should be unique
        public static readonly string LatencyTurnName = "Turn";
        // TODO can't deteremine. So parent could not be used
        public static readonly string LatencyAuthName = "GetAuthToken";

        private readonly BotSettingsBase _settings;
        private readonly IBotTelemetryClient _botTelemetryClient;
        private readonly string _label;

        public LatencyMiddleware(
            BotSettingsBase settings,
            IBotTelemetryClient botTelemetryClient,
            string label)
        {
            _settings = settings;
            _botTelemetryClient = botTelemetryClient;
            _label = label;

            if (string.IsNullOrEmpty(_label))
            {
                throw new Exception("_label should not be null or empty");
            }
        }

        public async Task OnTurnAsync(ITurnContext turnContext, NextDelegate next, CancellationToken cancellationToken = default(CancellationToken))
        {
            // hook up onSend pipeline
            turnContext.OnSendActivities(async (ctx, activities, nextSend) =>
            {
                var stopwatch = new Stopwatch();
                stopwatch.Start();

                // run full pipeline
                var responses = await nextSend().ConfigureAwait(false);

                _botTelemetryClient.TrackLatency(ctx, stopwatch, "SendActivities");

                return responses;
            });

            turnContext.TurnState.Add(LatencyLabelKey, _label);
            turnContext.TurnState.Add(LatencyParentKey, new LinkedList<string>());
            turnContext.PushParent(LatencyTurnName);

            var turnStopwatch = new Stopwatch();
            turnStopwatch.Start();

            await next(cancellationToken).ConfigureAwait(false);

            _botTelemetryClient.TrackLatency(turnContext, turnStopwatch, LatencyTurnName, true);
        }
    }

    public static class IBotTelemetryClientExtensions
    {
        public static void TrackLatency(this IBotTelemetryClient botTelemetryClient, ITurnContext turnContext, Stopwatch stopwatch, string name, bool popParent = false)
        {
            if (turnContext == null)
            {
                return;
            }

            var latencyParent = turnContext.TurnState.Get<LinkedList<string>>(LatencyMiddleware.LatencyParentKey);

            if (popParent)
            {
                latencyParent.RemoveLast();
            }

            botTelemetryClient.TrackLatency(turnContext, stopwatch, name, latencyParent.Count == 0 ? string.Empty : latencyParent.Last.Value);
        }

        public static void TrackLatency(this IBotTelemetryClient botTelemetryClient, ITurnContext turnContext, Stopwatch stopwatch, string name, string parent)
        {
            if (turnContext == null)
            {
                return;
            }

            stopwatch.Stop();

            var turnName = string.Empty;

            if (name == LatencyMiddleware.LatencyTurnName)
            {
                turnName = turnContext.TurnState.Get<string>(LatencyMiddleware.LatencyTurnNameKey);
            }

            botTelemetryClient.TrackEvent(turnContext.GetLatencyEventName(name),
                new Dictionary<string, string>
                {
                    // TODO should not be null or empty
                    { "LatencyParent", turnContext.GetLatencyEventName(parent) },
                    // TODO only set one for one *whole* turn. In this way, we could set it in skill instead of VA
                    { "LatencyTurnName", string.IsNullOrEmpty(turnName) ? string.Empty : turnContext.GetLatencyEventName(turnName) },
                },
                new Dictionary<string, double>
                {
                    { "Latency", stopwatch.ElapsedMilliseconds },
                });
        }
    }

    public static class ITurnContextExtensions
    {
        // LabelName
        public static string GetLatencyEventName(this ITurnContext turnContext, string name)
        {
            var latencyLabel = turnContext.TurnState.Get<string>(LatencyMiddleware.LatencyLabelKey);
            return $"{latencyLabel}{name}";
        }

        public static void PushParent(this ITurnContext turnContext, string name)
        {
            var latencyParent = turnContext.TurnState.Get<LinkedList<string>>(LatencyMiddleware.LatencyParentKey);
            latencyParent.AddLast(name);
        }

        public static void SetTurnName(this ITurnContext turnContext, string name)
        {
            if (turnContext.TurnState.ContainsKey(LatencyMiddleware.LatencyTurnNameKey))
            {
                turnContext.TurnState[LatencyMiddleware.LatencyTurnNameKey] = name;
            }
            else
            {
                turnContext.TurnState.Add(LatencyMiddleware.LatencyTurnNameKey, name);
            }
        }
    }

    public static class LuisRecognizerExtensions
    {
        public static async Task<T> RecognizeAsync<T>(this LuisRecognizer recognizer, string name, ITurnContext turnContext, CancellationToken cancellationToken, IBotTelemetryClient botTelemetryClient) where T : IRecognizerConvert, new()
        {
            var luisName = $"Luis{name}";

            turnContext.PushParent(luisName);

            var stopWatch = new Stopwatch();
            stopWatch.Start();

            var result = await recognizer.RecognizeAsync<T>(turnContext, cancellationToken);

            botTelemetryClient.TrackLatency(turnContext, stopWatch, luisName, true);

            return result;
        }

        public static async Task<T> RecognizeAsync<T>(this Dictionary<string, LuisRecognizer> recognizer, string name, ITurnContext turnContext, CancellationToken cancellationToken, IBotTelemetryClient botTelemetryClient) where T : IRecognizerConvert, new()
        {
            return await recognizer[name].RecognizeAsync<T>(name, turnContext, cancellationToken, botTelemetryClient);
        }
    }
}
