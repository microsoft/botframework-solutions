// https://docs.microsoft.com/en-us/visualstudio/modeling/t4-include-directive?view=vs-2017
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.IO;
using System.Runtime.CompilerServices;
using Microsoft.Bot.Solutions.Dialogs;

namespace Microsoft.Bot.Solutions.Tests.Resources
{
    /// <summary>
    /// Contains bot responses.
    /// </summary>
    public static class TestResponses
    {
        private static readonly ResponseManager _responseManager;

		static TestResponses()
        {
            var dir = Path.GetDirectoryName(typeof(TestResponses).Assembly.Location);
            var resDir = Path.Combine(dir, @"Resources");
            _responseManager = new ResponseManager(resDir, "TestResponses");
        }

        // Generated accessors  
        public static BotResponse GetResponseText => GetBotResponse();
                
        private static BotResponse GetBotResponse([CallerMemberName] string propertyName = null)
        {
            return _responseManager.GetBotResponse(propertyName);
        }
    }
}