using System;
using System.Collections.Generic;
using Microsoft.Bot.Schema;
using Newtonsoft.Json;

namespace ToDoSkill.Utilities
{
    public class ToDoCommonUtil
    {
        public const int DefaultDisplaySize = 4;

        public static Activity GetToDoResponseActivity(string text)
        {
            var activity = new Activity()
            {
                Type = ActivityTypes.Message,
                Text = text,
                Speak = text
            };
            return activity;
        }
    }
}
