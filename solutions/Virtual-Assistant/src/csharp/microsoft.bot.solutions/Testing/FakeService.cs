// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Bot.Solutions.Testing
{
    using System;
    using System.Collections.Generic;

    public abstract class FakeService
    {
        private readonly Dictionary<string, object> resultCache = new Dictionary<string, object>();

        public void SetupResult(string methodName, object result)
        {
            this.resultCache.Add(methodName, result);
        }

        public bool TryGetCachedResult<T>(string methodName, out T result)
        {
            if (this.resultCache.ContainsKey(methodName))
            {
                var cached = this.resultCache[methodName];
                if (cached.GetType() == typeof(Exception))
                {
                    throw (Exception)cached;
                }

                result = (T)cached;
                this.resultCache.Remove(methodName);
                return true;
            }

            result = default(T);
            return false;
        }
    }
}