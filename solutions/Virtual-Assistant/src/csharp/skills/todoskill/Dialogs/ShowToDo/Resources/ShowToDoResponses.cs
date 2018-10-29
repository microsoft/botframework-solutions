  
// https://docs.microsoft.com/en-us/visualstudio/modeling/t4-include-directive?view=vs-2017
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.IO;
using System.Runtime.CompilerServices;
using Microsoft.Bot.Solutions.Dialogs;

namespace ToDoSkill.Dialogs.ShowToDo.Resources
{
    /// <summary>
    /// Contains bot responses.
    /// </summary>
    public static class ShowToDoResponses
    {
        private static readonly ResponseManager _responseManager;

		static ShowToDoResponses()
        {
            var dir = Path.GetDirectoryName(typeof(ShowToDoResponses).Assembly.Location);
            var resDir = Path.Combine(dir, @"Dialogs\ShowToDo\Resources");
            _responseManager = new ResponseManager(resDir, "ShowToDoResponses");
        }

        // Generated accessors  
        public static BotResponse ReadToDoTasks => GetBotResponse();
          
        public static BotResponse ShowNextToDoTasks => GetBotResponse();
          
        public static BotResponse ShowPreviousToDoTasks => GetBotResponse();
          
        public static BotResponse ShowingMoreTasks => GetBotResponse();
          
        public static BotResponse NoToDoTasksPrompt => GetBotResponse();
                
        private static BotResponse GetBotResponse([CallerMemberName] string propertyName = null)
        {
            return _responseManager.GetBotResponse(propertyName);
        }
    }
}