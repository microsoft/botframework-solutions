// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;

namespace $safeprojectname$.Middleware.Telemetry
{
    public interface ITelemetryLuisRecognizer : IRecognizer
    {
        bool LogPersonalInformation { get; }

        Task<T> RecognizeAsync<T>(DialogContext dialogContext, CancellationToken cancellationToken = default(CancellationToken))
            where T : IRecognizerConvert, new();

        new Task<T> RecognizeAsync<T>(ITurnContext turnContext, CancellationToken cancellationToken = default(CancellationToken))
            where T : IRecognizerConvert, new();
    }
}