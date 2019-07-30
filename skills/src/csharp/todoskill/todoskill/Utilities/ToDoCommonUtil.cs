using System;
using System.Collections.Generic;
using Microsoft.Bot.Schema;
using Newtonsoft.Json;

namespace ToDoSkill.Utilities
{
    public class ToDoCommonUtil
    {
        public const int DefaultDisplaySize = 4;

        public static Activity GetToDoResponseActivity(string json)
        {
            var activity = new Activity()
            {
                Type = ActivityTypes.Message,
                Text = "Sorry, I didn't understand what you meant.",
                Speak = "Sorry, I didn't understand what you meant.",
                InputHint = InputHints.AcceptingInput
            };
            try
            {
                dynamic template = JsonConvert.DeserializeObject(json);

                if (template.replies != null)
                {
                    var replies = template.replies;
                    int num = replies.Count;
                    var reply = replies[new Random().Next(0, num)];
                    activity.Text = (string)reply.text;
                    activity.Speak = (string)reply.speak;
                }

                if (template.inputHint != null)
                {
                    activity.InputHint = (string)template.inputHint;
                }

                if (template.suggestedActions != null)
                {
                    activity.SuggestedActions = new SuggestedActions()
                    {
                        Actions = new List<CardAction>()
                    };
                    var suggestedActions = template.suggestedActions;
                    for (int i = 0; i < suggestedActions.Count; i++)
                    {
                        var suggestedAction = (string)suggestedActions[i];
                        activity.SuggestedActions.Actions.Add(new CardAction(type: ActionTypes.ImBack, title: suggestedAction, value: suggestedAction));
                    }
                }
            }
            catch
            {
            }

            return activity;
        }
    }
}
