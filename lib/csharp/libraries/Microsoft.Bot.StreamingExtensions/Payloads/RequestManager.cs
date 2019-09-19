// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Bot.StreamingExtensions.Payloads
{
    internal class RequestManager : IRequestManager
    {
        private readonly ConcurrentDictionary<Guid, TaskCompletionSource<ReceiveResponse>> _responseTasks;

        public RequestManager()
            : this(new ConcurrentDictionary<Guid, TaskCompletionSource<ReceiveResponse>>())
        {
        }

        public RequestManager(ConcurrentDictionary<Guid, TaskCompletionSource<ReceiveResponse>> responseTasks)
        {
            _responseTasks = responseTasks;
        }

        public Task<bool> SignalResponse(Guid requestId, ReceiveResponse response)
        {
            if (_responseTasks.TryGetValue(requestId, out TaskCompletionSource<ReceiveResponse> signal))
            {
                Task.Run(() => { signal.TrySetResult(response); });
                return Task.FromResult(true);
            }

            return Task.FromResult(false);
        }

        public async Task<ReceiveResponse> GetResponseAsync(Guid requestId, CancellationToken cancellationToken)
        {
            TaskCompletionSource<ReceiveResponse> responseTask = new TaskCompletionSource<ReceiveResponse>();

            if (!_responseTasks.TryAdd(requestId, responseTask))
            {
                return null;
            }

            if (cancellationToken == null)
            {
                cancellationToken = CancellationToken.None;
            }

            try
            {
                using (cancellationToken.Register(() =>
                {
                    responseTask.TrySetCanceled();
                }))
                {
                    var response = await responseTask.Task.ConfigureAwait(false);
                    return response;
                }
            }
            finally
            {
                _responseTasks.TryRemove(requestId, out responseTask);
            }
        }
    }
}
