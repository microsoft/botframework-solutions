// https://docs.microsoft.com/en-us/visualstudio/modeling/t4-include-directive?view=vs-2017
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System.IO;
using System.Runtime.CompilerServices;
using Microsoft.Bot.Solutions.Dialogs;

namespace ToDoSkill.Dialogs.AddToDo.Resources
{
    /// <summary>
    /// Contains bot responses.
    /// </summary>
    public static class AddToDoResponses
    {
        private static readonly ResponseManager _responseManager;

        static AddToDoResponses()
        {
            var dir = Path.GetDirectoryName(typeof(AddToDoResponses).Assembly.Location);
            var resDir = Path.Combine(dir, @"Dialogs\AddToDo\Resources");
            _responseManager = new ResponseManager(resDir, "AddToDoResponses");
        }

        // Generated accessors
        public static BotResponse AskTaskContentText => GetBotResponse();

        public static BotResponse AfterTaskAdded => GetBotResponse();

        public static BotResponse SwitchListType => GetBotResponse();

        public static BotResponse AddMoreTask => GetBotResponse();

        public static BotResponse AskAddDupTaskPrompt => GetBotResponse();

        private static BotResponse GetBotResponse([CallerMemberName] string propertyName = null)
        {
            return _responseManager.GetBotResponse(propertyName);
        }
    }
}